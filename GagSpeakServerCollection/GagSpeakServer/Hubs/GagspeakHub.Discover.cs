using GagspeakAPI;
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

    public async Task<bool> UploadMoodle(MoodleUploadDto dto)
    {
        _logger.LogCallInfo();

        // if the guid is empty, it's not a valid pattern.
        if (dto.MoodleInfo.MoodleStatus.GUID == Guid.Empty)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Invalid Moodle Identifier.").ConfigureAwait(false);
            return false;
        }

        // ensure the uploaing User is the userUID
        if(!string.Equals(dto.Publisher.UID, UserUID, StringComparison.Ordinal))
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "You are not authorized to upload this moodle.").ConfigureAwait(false);
            return false;
        }

        // Attempt to prevent reuploads and duplicate uploads.
        var existingMoodle = await DbContext.Moodles.SingleOrDefaultAsync(p => p.Identifier == dto.MoodleInfo.MoodleStatus.GUID).ConfigureAwait(false);
        if (existingMoodle is not null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Moodle already exists.").ConfigureAwait(false);
            return false;
        }

        /////////////// Step 1: Check and add tags //////////////////
        // ENSURE THE TAGS ARE LOWERCASE.
        var uploadTagsLower = dto.MoodleInfo.Tags.Select(t => t.ToLowerInvariant()).ToList();
        // Get all existing tags from the database
        var existingTags = await DbContext.Keywords.Where(t => uploadTagsLower.Contains(t.Word)).ToListAsync().ConfigureAwait(false);
        // Get the new tags that are not in the database
        var newTags = dto.MoodleInfo.Tags.Except(existingTags.Select(t => t.Word), StringComparer.OrdinalIgnoreCase).ToList();
        // Create and insert the new tags not yet in DB.
        foreach (var newTagName in newTags)
        {
            var newTag = new Keyword { Word = newTagName };
            DbContext.Keywords.Add(newTag);
            existingTags.Add(newTag);
        }

        ///////////// Step 2: Create and insert the new Pattern Entry /////////////
        MoodleStatus newMoodleEntry = new()
        {
            
            Identifier = dto.MoodleInfo.MoodleStatus.GUID,
            PublisherUID = UserUID,
            TimePublished = DateTime.UtcNow,
            Author = dto.MoodleInfo.Author,
            MoodleKeywords = new List<MoodleKeyword>(),
            // moodle information.
            IconID = dto.MoodleInfo.MoodleStatus.IconID,
            Title = dto.MoodleInfo.MoodleStatus.Title,
            Description = dto.MoodleInfo.MoodleStatus.Description,
            Type = dto.MoodleInfo.MoodleStatus.Type,
            Dispelable = dto.MoodleInfo.MoodleStatus.Dispelable,
            Stacks = dto.MoodleInfo.MoodleStatus.Stacks,
            Persistent = dto.MoodleInfo.MoodleStatus.Persistent,
            Days = dto.MoodleInfo.MoodleStatus.Days,
            Hours = dto.MoodleInfo.MoodleStatus.Hours,
            Minutes = dto.MoodleInfo.MoodleStatus.Minutes,
            Seconds = dto.MoodleInfo.MoodleStatus.Seconds,
            NoExpire = dto.MoodleInfo.MoodleStatus.NoExpire,
            AsPermanent = dto.MoodleInfo.MoodleStatus.AsPermanent,
            StatusOnDispell = dto.MoodleInfo.MoodleStatus.StatusOnDispell,
            CustomVFXPath = dto.MoodleInfo.MoodleStatus.CustomVFXPath,
            StackOnReapply = dto.MoodleInfo.MoodleStatus.StackOnReapply
        };
        DbContext.Moodles.Add(newMoodleEntry);

        ///////////// Step 3: Create and insert the new Pattern Entry Tags /////////////
        foreach (var tag in existingTags)
        {
            var moodleEntryTag = new MoodleKeyword
            {
                MoodleStatusId = newMoodleEntry.Identifier,
                KeywordWord = tag.Word
            };
            DbContext.MoodleKeywords.Add(moodleEntryTag);
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
        IQueryable<PatternEntry> patternsQuery = DbContext.Patterns.AsQueryable();

        // 1. Apply title or author / title filters
        if (!string.IsNullOrEmpty(dto.SearchString))
        {
            patternsQuery = patternsQuery.Where(p =>
                // only match author if equal.
                p.Author.Equals(dto.SearchString, StringComparison.OrdinalIgnoreCase) ||
                // or if it is contained within the title.
                p.Name.Contains(dto.SearchString, StringComparison.OrdinalIgnoreCase));
         
        }

        // 2. Apply tag filters (only if tags are provided)
        if (dto.Tags is not null && dto.Tags.Any())
        {
            // Include the MoodleKeywords for tag filtering (Only include here if we know we are using them)
            patternsQuery = patternsQuery.Include(p => p.PatternKeywords).AsSplitQuery()
                .Where(p => p.PatternKeywords.Any(t => dto.Tags.Contains(t.KeywordWord)));
        }

        // finalize the filtered results against our filter and order for sorting.
        switch (dto.Filter)
        {
            case ResultFilter.MostRecent:
                patternsQuery = dto.Sort == SearchSort.Ascending
                    ? patternsQuery.OrderBy(p => p.TimePublished)
                    : patternsQuery.OrderByDescending(p => p.TimePublished);
                break;
            case ResultFilter.Downloads:
                patternsQuery = dto.Sort == SearchSort.Ascending
                    ? patternsQuery.OrderBy(p => p.DownloadCount)
                    : patternsQuery.OrderByDescending(p => p.DownloadCount);
                break;
            case ResultFilter.Likes:
                patternsQuery = dto.Sort == SearchSort.Ascending
                    ? patternsQuery.Include(p => p.UserPatternLikes).AsSplitQuery().OrderBy(p => p.LikeCount)
                    : patternsQuery.Include(p => p.UserPatternLikes).AsSplitQuery().OrderByDescending(p => p.LikeCount);
                break;
        }
        // limit the results to 30 patterns.
        var patterns = await patternsQuery.Take(30).Include(p => p.PatternKeywords).Include(p => p.UserPatternLikes).ToListAsync().ConfigureAwait(false);

        // Check if patterns is null or contains null entries
        if (patterns is null || patterns.Any(p => p is null))
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

    public async Task<List<ServerMoodleInfo>> SearchMoodles(MoodleSearchDto dto)
    {
        _logger.LogCallInfo();
        // ensure they are a valid user.
        var user = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false);
        if (user is null)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "User not found.").ConfigureAwait(false);
            return new List<ServerMoodleInfo>();
        }

        // Start with a base query without including sub-dependencies initially
        IQueryable<MoodleStatus> moodlesQuery = DbContext.Moodles.AsQueryable();

        // 1. Apply title or author / title filters
        if (!string.IsNullOrEmpty(dto.SearchString))
        {
            {
                moodlesQuery = moodlesQuery.Where(p =>
                    // only match author if equal.
                    p.Author.Equals(dto.SearchString, StringComparison.OrdinalIgnoreCase) ||
                    // or if it is contained within the title.
                    p.Title.Contains(dto.SearchString, StringComparison.OrdinalIgnoreCase));
            }
        }

        // 2. Apply tag filters (only if tags are provided)
        if (dto.Tags != null && dto.Tags.Any())
        {
            // Include the MoodleKeywords for tag filtering (Only include here if we know we are using them)
            moodlesQuery = moodlesQuery.Include(p => p.MoodleKeywords).AsSplitQuery()
                .Where(p => p.MoodleKeywords.Any(t => dto.Tags.Contains(t.KeywordWord)));
        }

        // 3. Apply sorting
        moodlesQuery = dto.Filter is ResultFilter.Likes
            ? (dto.Sort is SearchSort.Ascending
                ? moodlesQuery.Include(p => p.LikesMoodles).OrderBy(p => p.LikeCount)
                : moodlesQuery.Include(p => p.LikesMoodles).OrderByDescending(p => p.LikeCount))
            : (dto.Sort is SearchSort.Ascending
                ? moodlesQuery.OrderBy(p => p.TimePublished)
                : moodlesQuery.OrderByDescending(p => p.TimePublished));


        // 4. Limit results (only run the this moodle keyword include to the first 50.
        var moodles = await moodlesQuery.Take(50).Include(p => p.MoodleKeywords).Include(p => p.LikesMoodles).ToListAsync().ConfigureAwait(false);

        // Check if final result is null or contains null entries
        if (moodles is null || moodles.Any(p => p is null))
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "No moodles found or some moodles are invalid.").ConfigureAwait(false);
            return new List<ServerMoodleInfo>();
        }


        // Convert to ServerMoodleInfo
        var result = moodles.Select(p => new ServerMoodleInfo
        {
            Likes = p.LikeCount,
            HasLikedMoodle = p.LikesMoodles.Any(likes => string.Equals(likes.UserUID, user.UID, StringComparison.Ordinal)),
            Author = p.Author,
            Tags = p.MoodleKeywords.Select(t => t.KeywordWord).ToList(),
            MoodleStatus = (
                p.Identifier,
                p.IconID,
                p.Title,
                p.Description,
                p.Type,
                string.Empty,
                p.Dispelable,
                p.Stacks,
                p.Persistent,
                p.Days,
                p.Hours,
                p.Minutes,
                p.Seconds,
                p.NoExpire,
                p.AsPermanent,
                p.StatusOnDispell,
                p.CustomVFXPath,
                p.StackOnReapply
                ),
        }).ToList();

        return result;
    }

    // FetchSearchTags
    public async Task<List<string>> FetchSearchTags()
    {
        _logger.LogCallInfo();
        var tags = await DbContext.Keywords.Select(k => k.Word).ToListAsync().ConfigureAwait(false);
        return tags;
    }
}

