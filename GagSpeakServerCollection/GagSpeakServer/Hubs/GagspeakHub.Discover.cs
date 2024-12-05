using GagspeakAPI.Data;
using GagspeakAPI.Dto.Patterns;
using GagspeakAPI.Dto.Toybox;
using GagspeakAPI.Enums;
using GagspeakShared.Models;
using Microsoft.EntityFrameworkCore;

namespace GagspeakServer.Hubs;

/// <summary>
/// Hosts the structure for the global chat hub and the pattern accessor list.
/// </summary>
public partial class GagspeakHub
{
    private const string GagspeakGlobalChat = "GlobalGagspeakChat";

    // really not a good idea to log this lol.
    public async Task SendGlobalChat(GlobalChatMessageDto message)
    {
        await Clients.Group(GagspeakGlobalChat).Client_GlobalChatMessage(message).ConfigureAwait(false);
    }

    public async Task<bool> UploadPattern(PatternUploadDto dto)
    {
        _logger.LogCallInfo();

        // if the guid is empty, it's not a valid pattern.
        if (dto.patternInfo.Identifier == Guid.Empty)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Invalid Pattern Identifier.").ConfigureAwait(false);
            return false;
        }

        // ensure the right person is doing this and that they exist.
        var user = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == dto.User.UID).ConfigureAwait(false);
        if (!string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal) || user == null
          || !string.Equals(dto.User.UID, user.UID, StringComparison.Ordinal))
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning,
                "Your User Doesnt exist, or you're trying to upload under someone else's name.").ConfigureAwait(false);
            return false;
        }

        // Attempt to prevent reuploads and duplicate uploads.
        var existingPattern = await DbContext.Patterns.SingleOrDefaultAsync(p => p.Identifier == dto.patternInfo.Identifier ||
        (p.Name == dto.patternInfo.Name && p.Length == dto.patternInfo.Length)).ConfigureAwait(false);
        if (existingPattern != null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Pattern already exists.").ConfigureAwait(false);
            return false;
        }

        // determine max upload allowance
        int maxUploadsPerWeek = dto.User.SupporterTier switch
        {
            CkSupporterTier.KinkporiumMistress => 999999,
            CkSupporterTier.DistinguishedConnoisseur => 20,
            CkSupporterTier.EsteemedPatron => 15,
            _ => 10
        };

        // Check if the user has exceeded the upload limit
        if (user.UploadLimitCounter >= maxUploadsPerWeek)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning,
                $"Upload limit exceeded. You can only upload {maxUploadsPerWeek} patterns per week.").ConfigureAwait(false);
            return false;
        }

        // Otherwise, update the upload counter and timestamp
        if (user.UploadLimitCounter == 0) user.FirstUploadTimestamp = DateTime.UtcNow;
        user.UploadLimitCounter++;

        // save the user data on the DB.
        DbContext.Users.Update(user);

        /////////////// Step 1: Check and add tags //////////////////
        // Get all existing tags from the database
        var existingTags = await DbContext.Keywords.Where(t => dto.patternInfo.Tags.Contains(t.Word)).ToListAsync().ConfigureAwait(false);
        // Get the new tags that are not in the database
        var newTags = dto.patternInfo.Tags.Except(existingTags.Select(t => t.Word), StringComparer.Ordinal).ToList();
        // Create and insert the new tags not yet in DB.
        foreach (var newTagName in newTags)
        {
            var newTag = new Keyword { Word = newTagName };
            DbContext.Keywords.Add(newTag);
            existingTags.Add(newTag);
        }

        ///////////// Step 2: Create and insert the new Pattern Entry /////////////
        PatternEntry newPatternEntry = new()
        {
            Identifier = dto.patternInfo.Identifier,
            PublisherUID = dto.User.UID,
            TimePublished = DateTime.UtcNow,
            Name = dto.patternInfo.Name,
            Description = dto.patternInfo.Description,
            Author = dto.patternInfo.Author,
            PatternKeywords = new List<PatternKeyword>(),
            DownloadCount = 0,
            UserPatternLikes = new List<LikesPatterns>(),
            ShouldLoop = dto.patternInfo.Looping,
            Length = dto.patternInfo.Length,
            Base64PatternData = dto.base64PatternData,
        };
        DbContext.Patterns.Add(newPatternEntry);

        ///////////// Step 3: Create and insert the new Pattern Entry Tags /////////////
        foreach (var tag in existingTags)
        {
            var patternEntryTag = new PatternKeyword
            {
                PatternEntryId = newPatternEntry.Identifier,
                KeywordWord = tag.Word
            };
            DbContext.PatternKeywords.Add(patternEntryTag);
        }

        // Save the user with the new upload log
        await DbContext.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }

    public async Task<bool> RemovePattern(Guid patternId)
    {
        _logger.LogCallInfo();

        // Ensure they are a valid user.
        var user = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false);
        if (user == null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "User not found.").ConfigureAwait(false);
            return false;
        }

        // Find the pattern by GUID and ensure it belongs to the user
        var pattern = await DbContext.Patterns.SingleOrDefaultAsync(p => p.Identifier == patternId && p.PublisherUID == user.UID).ConfigureAwait(false);

        if (pattern == null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning,
                "Pattern not found or you do not have permission to delete it.").ConfigureAwait(false);
            return false;
        }

        // Find and remove related PatternEntryTags
        var patternEntryTags = await DbContext.PatternKeywords.Where(pet => pet.PatternEntryId == patternId).ToListAsync().ConfigureAwait(false);
        DbContext.PatternKeywords.RemoveRange(patternEntryTags);

        // Remove the pattern
        DbContext.Patterns.Remove(pattern);

        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Information, "Pattern removed successfully.").ConfigureAwait(false);
        return true;
    }

    public async Task<bool> RemoveMoodle(Guid moodleId)
    {
        _logger.LogCallInfo();

        // Ensure they are a valid user.
        var user = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false);
        if (user is null) return false;

        // Find the pattern by GUID and ensure it belongs to the user
        var moodle = await DbContext.Moodles.SingleOrDefaultAsync(p => p.Identifier == moodleId && p.PublisherUID == user.UID).ConfigureAwait(false);
        if (moodle is null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Pattern not found or it's not yours.").ConfigureAwait(false);
            return false;
        }

        // Find and remove related keywords mapping from the Keywords table and the MoodleStautsId
        var moodleKeywords = await DbContext.MoodleKeywords.Where(pet => pet.MoodleStatusId == moodleId).ToListAsync().ConfigureAwait(false);
        DbContext.MoodleKeywords.RemoveRange(moodleKeywords);
        DbContext.Moodles.Remove(moodle);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }

    public async Task<string> DownloadPattern(Guid patternId)
    {
        _logger.LogCallInfo();
        // locate the pattern in the database by its GUID.
        var pattern = await DbContext.Patterns.SingleOrDefaultAsync(p => p.Identifier == patternId).ConfigureAwait(false);
        if (pattern == null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Pattern not found.").ConfigureAwait(false);
            return string.Empty;
        }

        // increment the download count for the pattern
        pattern.DownloadCount++;
        DbContext.Patterns.Update(pattern);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);
        return pattern.Base64PatternData;
    }

    public async Task<bool> LikePattern(Guid patternId)
    {
        _logger.LogCallInfo();
        // Get the current user
        var user = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false);
        if (user is null) return false;
        // Locate the pattern in the database by its GUID.
        var pattern = await DbContext.Patterns.SingleOrDefaultAsync(p => p.Identifier == patternId).ConfigureAwait(false);
        if (pattern is null) return false;

        // Check if the user has already liked this pattern
        var existingLike = await DbContext.LikesPatterns.SingleAsync(upl => upl.PatternEntryId == patternId && upl.UserUID == user.UID).ConfigureAwait(false);
        if (existingLike is not null)
        {
            // User has already liked this pattern, so remove the like
            DbContext.LikesPatterns.Remove(existingLike);
        }
        else
        {
            // User has not liked this pattern, so add a new like
            var userPatternLike = new LikesPatterns
            {
                UserUID = user.UID,
                User = user,
                PatternEntryId = patternId,
                PatternEntry = pattern
            };
            DbContext.LikesPatterns.Add(userPatternLike);
        }
        // Save changes to the database
        await DbContext.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }

    public async Task<bool> LikeMoodle(Guid moodleId)
    {
        _logger.LogCallInfo();
        // Get the current user
        var user = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false);
        if (user is null) return false;
        // Locate the pattern in the database by its GUID.
        var moodle = await DbContext.Moodles.SingleOrDefaultAsync(p => p.Identifier == moodleId).ConfigureAwait(false);
        if (moodle is null) return false;

        // Check if the user has already liked this pattern
        var existingLike = await DbContext.LikesMoodles.SingleAsync(upl => upl.MoodleStatusId == moodleId && upl.UserUID == user.UID).ConfigureAwait(false);
        if (existingLike is not null)
        {
            // User has already liked this pattern, so remove the like
            DbContext.LikesMoodles.Remove(existingLike);
        }
        else
        {
            // User has not liked this pattern, so add a new like
            var userPatternLike = new LikesMoodles
            {
                UserUID = user.UID,
                User = user,
                MoodleStatusId = moodleId,
                MoodleStatus = moodle
            };
            DbContext.LikesMoodles.Add(userPatternLike);
        }
        // Save changes to the database
        await DbContext.SaveChangesAsync().ConfigureAwait(false);
        return true;
    }

    public async Task<List<ServerPatternInfo>> SearchPatterns(PatternSearchDto dto)
    {
        _logger.LogCallInfo();
        // ensure they are a valid user.
        var user = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false);
        if (user == null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "User not found.").ConfigureAwait(false);
            return new List<ServerPatternInfo>();
        }

        // Start with a base query, insure we include the sub-dependencies such as the tags and likes.
        IQueryable<PatternEntry> patternsQuery = DbContext.Patterns
            .Include(p => p.PatternKeywords).Include(p => p.UserPatternLikes).AsSplitQuery();

        // Apply search string filter if provided (be it pattern name or author name.)
        if (!string.IsNullOrEmpty(dto.SearchString))
        {
            if (dto.Filter == SearchFilter.Author)
                patternsQuery = patternsQuery.Where(p => p.Author.Contains(dto.SearchString, StringComparison.OrdinalIgnoreCase));
            else
                patternsQuery = patternsQuery.Where(p => p.Name.Contains(dto.SearchString, StringComparison.OrdinalIgnoreCase));
        }

        // Apply tag filters if provided
        if (dto.Tags != null && dto.Tags.Any())
            patternsQuery = patternsQuery.Where(p => p.PatternKeywords.Any(t => dto.Tags.Contains(t.KeywordWord)));

        // finalize the filtered results against our filter and order for sorting.
        switch (dto.Filter)
        {
            case SearchFilter.MostRecent:
                patternsQuery = dto.Sort == SearchSort.Ascending
                    ? patternsQuery.OrderBy(p => p.TimePublished)
                    : patternsQuery.OrderByDescending(p => p.TimePublished);
                break;
            case SearchFilter.Downloads:
                patternsQuery = dto.Sort == SearchSort.Ascending
                    ? patternsQuery.OrderBy(p => p.DownloadCount)
                    : patternsQuery.OrderByDescending(p => p.DownloadCount);
                break;
            case SearchFilter.Likes:
                patternsQuery = dto.Sort == SearchSort.Ascending
                    ? patternsQuery.OrderBy(p => p.LikeCount)
                    : patternsQuery.OrderByDescending(p => p.LikeCount);
                break;
            case SearchFilter.UsesVibration:
                patternsQuery = patternsQuery.Where(p => p.UsesVibrations);
                break;
            case SearchFilter.UsesRotation:
                patternsQuery = patternsQuery.Where(p => p.UsesRotations);
                break;
            case SearchFilter.DurationTiny:
                patternsQuery = patternsQuery.Where(p => p.Length < TimeSpan.FromMinutes(1));
                patternsQuery = dto.Sort == SearchSort.Ascending
                    ? patternsQuery.OrderBy(p => p.Length)
                    : patternsQuery.OrderByDescending(p => p.Length);
                break;
            case SearchFilter.DurationShort:
                patternsQuery = patternsQuery.Where(p => p.Length <= TimeSpan.FromMinutes(5));
                patternsQuery = dto.Sort == SearchSort.Ascending
                    ? patternsQuery.OrderBy(p => p.Length)
                    : patternsQuery.OrderByDescending(p => p.Length);
                break;
            case SearchFilter.DurationMedium:
                patternsQuery = patternsQuery.Where(p => p.Length > TimeSpan.FromMinutes(5) && p.Length <= TimeSpan.FromMinutes(20));
                patternsQuery = dto.Sort == SearchSort.Ascending
                    ? patternsQuery.OrderBy(p => p.Length)
                    : patternsQuery.OrderByDescending(p => p.Length);
                break;
            case SearchFilter.DurationLong:
                patternsQuery = patternsQuery.Where(p => p.Length > TimeSpan.FromMinutes(20) && p.Length <= TimeSpan.FromMinutes(60));
                patternsQuery = dto.Sort == SearchSort.Ascending
                    ? patternsQuery.OrderBy(p => p.Length)
                    : patternsQuery.OrderByDescending(p => p.Length);
                break;
            case SearchFilter.DurationExtraLong:
                patternsQuery = patternsQuery.Where(p => p.Length > TimeSpan.FromMinutes(60));
                patternsQuery = dto.Sort == SearchSort.Ascending
                    ? patternsQuery.OrderBy(p => p.Length)
                    : patternsQuery.OrderByDescending(p => p.Length);
                break;
        }
        // limit the results to 30 patterns.
        var patterns = await patternsQuery.Take(30).ToListAsync().ConfigureAwait(false);

        // Check if patterns is null or contains null entries
        if (patterns == null || patterns.Any(p => p == null))
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "No patterns found or some patterns are invalid.").ConfigureAwait(false);
            return new List<ServerPatternInfo>();
        }

        // Convert to ServerPatternInfo
        var result = patterns.Select(p => new ServerPatternInfo
        {
            Identifier = p.Identifier,
            Name = p.Name,
            Description = p.Description,
            Author = p.Author,
            Tags = p.PatternKeywords.Select(t => t.KeywordWord).ToList(),
            Downloads = p.DownloadCount,
            Likes = p.LikeCount,
            Looping = p.ShouldLoop,
            Length = p.Length,
            UploadedDate = p.TimePublished,
            UsesVibrations = p.UsesVibrations,
            UsesRotations = p.UsesRotations,
            HasLiked = p.UserPatternLikes.Any(upl => string.Equals(upl.UserUID, user.UID, StringComparison.Ordinal))
        }).ToList();

        return result;
    }
}

