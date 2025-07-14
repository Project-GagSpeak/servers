using GagspeakAPI.Data;
using GagspeakAPI.Dto.Sharehub;
using GagspeakAPI.Enums;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakShared.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace GagspeakServer.Hubs;
#pragma warning disable MA0011 // IFormatProvider is missing

/// <summary>
/// Hosts the structure for the global chat hub and the pattern accessor list.
/// </summary>
public partial class GagspeakHub
{
    public async Task<HubResponse> UploadPattern(PatternUpload dto)
    {
        _logger.LogCallInfo();

        // if the guid is empty, it's not a valid pattern.
        if (dto.patternInfo.Identifier == Guid.Empty)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "Invalid Pattern Identifier.").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);
        }

        // ensure the right person is doing this and that they exist.
        User user = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false);
        if (user is null || !string.Equals(user.UID, UserUID, StringComparison.Ordinal))
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "Your User Doesn't exist.").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);
        }

        // Attempt to prevent reuploads and duplicate uploads.
        PatternEntry existingPattern = await DbContext.Patterns
            .SingleOrDefaultAsync(p => p.Identifier == dto.patternInfo.Identifier || (p.Name == dto.patternInfo.Label && p.Length == dto.patternInfo.Length))
            .ConfigureAwait(false);
        if (existingPattern is not null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "Pattern already exists.").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.DuplicateEntry);
        }

        // determine max upload allowance
        int maxUploadsPerWeek = user.VanityTier switch
        {
            CkSupporterTier.KinkporiumMistress => 999999,
            CkSupporterTier.DistinguishedConnoisseur => 20,
            CkSupporterTier.EsteemedPatron => 15,
            _ => 10
        };

        // Check if the user has exceeded the upload limit
        if (user.UploadLimitCounter >= maxUploadsPerWeek)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning,
                $"Upload limit exceeded. You can only upload {maxUploadsPerWeek} patterns per week.").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.UploadLimitExceeded);
        }

        // Otherwise, update the upload counter and timestamp
        if (user.UploadLimitCounter == 0) user.FirstUploadTimestamp = DateTime.UtcNow;
        user.UploadLimitCounter++;

        // save the user data on the DB.
        DbContext.Users.Update(user);

        /////////////// Step 1: Check and add tags //////////////////
        // ENSURE THE TAGS ARE LOWERCASE.
        List<string> uploadTagsLower = dto.patternInfo.Tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.ToLowerInvariant()).ToList();
        // Get all existing tags from the database
        List<Keyword> existingTags = await DbContext.Keywords.Where(t => uploadTagsLower.Contains(t.Word)).ToListAsync().ConfigureAwait(false);
        // Get the new tags that are not in the database
        List<string> newTags = dto.patternInfo.Tags.Except(existingTags.Select(t => t.Word), StringComparer.Ordinal).ToList();
        // Create and insert the new tags not yet in DB.
        foreach (string newTagName in newTags)
        {
            Keyword newTag = new Keyword { Word = newTagName };
            DbContext.Keywords.Add(newTag);
            existingTags.Add(newTag);
        }

        ///////////// Step 2: Create and insert the new Pattern Entry /////////////
        PatternEntry newPatternEntry = new()
        {
            Identifier = dto.patternInfo.Identifier,
            PublisherUID = UserUID,
            TimePublished = DateTime.UtcNow,
            Name = dto.patternInfo.Label,
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
        foreach (Keyword tag in existingTags)
        {
            PatternKeyword patternEntryTag = new PatternKeyword
            {
                PatternEntryId = newPatternEntry.Identifier,
                KeywordWord = tag.Word
            };
            DbContext.PatternKeywords.Add(patternEntryTag);
        }

        // Save the user with the new upload log
        await DbContext.SaveChangesAsync().ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    public async Task<HubResponse> UploadMoodle(MoodleUpload dto)
    {
        _logger.LogCallInfo();

        // if the guid is empty, it's not a valid pattern.
        if (dto.MoodleInfo.GUID == Guid.Empty)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "Invalid Moodle Identifier.").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);
        }

        // ensure the uploader is a valid user in the database.
        User user = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false);
        if (user is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "You are not authorized to upload this moodle.").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);
        }

        // Attempt to prevent reuploads and duplicate uploads.
        MoodleStatus existingMoodle = await DbContext.Moodles.SingleOrDefaultAsync(p => p.Identifier == dto.MoodleInfo.GUID).ConfigureAwait(false);
        if (existingMoodle is not null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "Moodle already exists.").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.DuplicateEntry);
        }

        /////////////// Step 1: Check and add tags //////////////////
        // ENSURE THE TAGS ARE LOWERCASE.
        List<string> uploadTagsLower = dto.Tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.ToLowerInvariant()).ToList();
        // Get all existing tags from the database
        List<Keyword> existingTags = await DbContext.Keywords.Where(t => uploadTagsLower.Contains(t.Word)).ToListAsync().ConfigureAwait(false);
        // Get the new tags that are not in the database
        List<string> newTags = dto.Tags.Except(existingTags.Select(t => t.Word), StringComparer.OrdinalIgnoreCase).ToList();
        // Create and insert the new tags not yet in DB.
        foreach (string newTagName in newTags)
        {
            Keyword newTag = new Keyword { Word = newTagName };
            DbContext.Keywords.Add(newTag);
            existingTags.Add(newTag);
        }

        ///////////// Step 2: Create and insert the new Pattern Entry /////////////
        MoodleStatus newMoodleEntry = new()
        {

            Identifier = dto.MoodleInfo.GUID,
            PublisherUID = UserUID,
            TimePublished = DateTime.UtcNow,
            Author = dto.AuthorName,
            MoodleKeywords = new List<MoodleKeyword>(),
            // moodle information.
            IconID = dto.MoodleInfo.IconID,
            Title = dto.MoodleInfo.Title,
            Description = dto.MoodleInfo.Description,
            Type = dto.MoodleInfo.Type,
            Dispelable = dto.MoodleInfo.Dispelable,
            Stacks = dto.MoodleInfo.Stacks,
            Persistent = dto.MoodleInfo.Persistent,
            Days = dto.MoodleInfo.Days,
            Hours = dto.MoodleInfo.Hours,
            Minutes = dto.MoodleInfo.Minutes,
            Seconds = dto.MoodleInfo.Seconds,
            NoExpire = dto.MoodleInfo.NoExpire,
            AsPermanent = dto.MoodleInfo.AsPermanent,
            StatusOnDispell = dto.MoodleInfo.StatusOnDispell,
            CustomVFXPath = dto.MoodleInfo.CustomVFXPath,
            StackOnReapply = dto.MoodleInfo.StackOnReapply
        };
        DbContext.Moodles.Add(newMoodleEntry);

        ///////////// Step 3: Create and insert the new Pattern Entry Tags /////////////
        foreach (Keyword tag in existingTags)
        {
            MoodleKeyword moodleEntryTag = new MoodleKeyword
            {
                MoodleStatusId = newMoodleEntry.Identifier,
                KeywordWord = tag.Word
            };
            DbContext.MoodleKeywords.Add(moodleEntryTag);
        }

        // Save the user with the new upload log
        await DbContext.SaveChangesAsync().ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }
    public async Task<HubResponse<string>> DownloadPattern(Guid patternId)
    {
        _logger.LogCallInfo();
        // locate the pattern in the database by its GUID.
        PatternEntry pattern = await DbContext.Patterns.SingleOrDefaultAsync(p => p.Identifier == patternId).ConfigureAwait(false);
        if (pattern is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "Pattern not found.").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.ShareHubEntryNotFound, string.Empty);
        }

        // increment the download count for the pattern
        pattern.DownloadCount++;
        DbContext.Patterns.Update(pattern);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);
        return HubResponseBuilder.Yippee(pattern.Base64PatternData);
    }

    public async Task<HubResponse> LikePattern(Guid patternId)
    {
        _logger.LogCallInfo();
        // Get the current user
        User user = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false);
        if (user is null) 
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Locate the pattern in the database by its GUID.
        PatternEntry pattern = await DbContext.Patterns.SingleOrDefaultAsync(p => p.Identifier == patternId).ConfigureAwait(false);
        if (pattern is null) 
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.ShareHubEntryNotFound);

        // Check if the user has already liked this pattern
        LikesPatterns existingLike = await DbContext.LikesPatterns.SingleOrDefaultAsync(upl => upl.PatternEntryId == patternId && upl.UserUID == user.UID).ConfigureAwait(false);
        if (existingLike is not null)
            DbContext.LikesPatterns.Remove(existingLike);
        else
        {
            // User has not liked this pattern, so add a new like
            LikesPatterns userPatternLike = new LikesPatterns
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
        return HubResponseBuilder.Yippee();
    }

    public async Task<HubResponse> LikeMoodle(Guid moodleId)
    {
        _logger.LogCallInfo();
        // Get the current user
        User user = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false);
        if (user is null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Locate the pattern in the database by its GUID.
        MoodleStatus moodle = await DbContext.Moodles.SingleOrDefaultAsync(p => p.Identifier == moodleId).ConfigureAwait(false);
        if (moodle is null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.ShareHubEntryNotFound);

        // Check if the user has already liked this pattern
        LikesMoodles existingLike = await DbContext.LikesMoodles.SingleOrDefaultAsync(upl => upl.MoodleStatusId == moodleId && upl.UserUID == user.UID).ConfigureAwait(false);
        if (existingLike is not null)
            DbContext.LikesMoodles.Remove(existingLike);
        else
        {
            // User has not liked this pattern, so add a new like
            LikesMoodles userPatternLike = new LikesMoodles
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
        return HubResponseBuilder.Yippee();
    }
    public async Task<HubResponse> RemovePattern(Guid patternId)
    {
        _logger.LogCallInfo();

        // Ensure they are a valid user.
        User user = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false);
        if (user is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "User not found.").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);
        }

        // Find the pattern by GUID and ensure it belongs to the user
        PatternEntry pattern = await DbContext.Patterns.SingleOrDefaultAsync(p => p.Identifier == patternId && p.PublisherUID == user.UID).ConfigureAwait(false);
        if (pattern is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "You don't have permission to delete it.").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPublisher);
        }

        // Find and remove related PatternEntryTags
        List<PatternKeyword> patternEntryTags = await DbContext.PatternKeywords.Where(pet => pet.PatternEntryId == patternId).ToListAsync().ConfigureAwait(false);
        DbContext.PatternKeywords.RemoveRange(patternEntryTags);

        // Remove the pattern
        DbContext.Patterns.Remove(pattern);

        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        await Clients.Caller.Callback_ServerMessage(MessageSeverity.Information, "Pattern removed successfully.").ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    public async Task<HubResponse> RemoveMoodle(Guid moodleId)
    {
        _logger.LogCallInfo();

        // Ensure they are a valid user.
        User user = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false);
        if (user is null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Find the pattern by GUID and ensure it belongs to the user
        MoodleStatus moodle = await DbContext.Moodles.SingleOrDefaultAsync(p => p.Identifier == moodleId && p.PublisherUID == user.UID).ConfigureAwait(false);
        if (moodle is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "Pattern not found or it's not yours.").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPublisher);
        }

        // Find and remove related keywords mapping from the Keywords table and the MoodleStautsId
        List<MoodleKeyword> moodleKeywords = await DbContext.MoodleKeywords.Where(pet => pet.MoodleStatusId == moodleId).ToListAsync().ConfigureAwait(false);
        DbContext.MoodleKeywords.RemoveRange(moodleKeywords);
        DbContext.Moodles.Remove(moodle);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    public async Task<HubResponse<List<ServerPatternInfo>>> SearchPatterns(PatternSearch dto)
    {
        _logger.LogCallInfo();
        // ensure they are a valid user.
        User user = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false);
        if (user is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "User not found.").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient, new List<ServerPatternInfo>());
        }

        // Start with a base query, insure we include the sub-dependencies such as the tags and likes.
        IQueryable<PatternEntry> patternsQuery = DbContext.Patterns.AsNoTracking().AsQueryable();
        // 1. Apply title or author / title filters
        if (!string.IsNullOrEmpty(dto.Input))
        {
            string searchStringLower = dto.Input.ToLower(CultureInfo.InvariantCulture);
            patternsQuery = patternsQuery.Where(p =>
                p.Author.ToLower().Equals(searchStringLower) ||
                p.Name.ToLower().Contains(searchStringLower));
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
            case HubFilter.DatePosted:
                patternsQuery = dto.Order is HubDirection.Ascending
                    ? patternsQuery.OrderBy(p => p.TimePublished)
                    : patternsQuery.OrderByDescending(p => p.TimePublished);
                break;
            case HubFilter.Downloads:
                patternsQuery = dto.Order is HubDirection.Ascending
                    ? patternsQuery.OrderBy(p => p.DownloadCount)
                    : patternsQuery.OrderByDescending(p => p.DownloadCount);
                break;
            case HubFilter.Likes:
                patternsQuery = dto.Order is HubDirection.Ascending
                    ? patternsQuery.OrderBy(p => p.UserPatternLikes.Count)
                    : patternsQuery.OrderByDescending(p => p.UserPatternLikes.Count);
                break;
        }

        // limit the results to 30 patterns.
        List<PatternEntry> patterns = await patternsQuery.Take(30).Include(p => p.PatternKeywords).Include(p => p.UserPatternLikes).AsSplitQuery().ToListAsync().ConfigureAwait(false);

        // Check if patterns is null or contains null entries
        if (patterns is null || patterns.Any(p => p is null))
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "No patterns found or some patterns are invalid.").ConfigureAwait(false);
            return HubResponseBuilder.Yippee(new List<ServerPatternInfo>());
        }

        // Convert to ServerPatternInfo
        List<ServerPatternInfo> result = patterns.Select(p => new ServerPatternInfo
        {
            Identifier = p.Identifier,
            Label = p.Name,
            Description = p.Description,
            Author = p.Author,
            Tags = p.PatternKeywords.Select(t => t.KeywordWord).ToHashSet(StringComparer.OrdinalIgnoreCase),
            Downloads = p.DownloadCount,
            Likes = p.LikeCount,
            Looping = p.ShouldLoop,
            Length = p.Length,
            UploadedDate = p.TimePublished,
            PrimaryDeviceUsed = p.PrimaryDeviceUsed,
            SecondaryDeviceUsed = p.SecondaryDeviceUsed,
            MotorsUsed = p.MotorsUsed,
            HasLiked = p.UserPatternLikes.Any(upl => string.Equals(upl.UserUID, user.UID, StringComparison.Ordinal))
        }).ToList();
        return HubResponseBuilder.Yippee(result);
    }

    public async Task<HubResponse<List<ServerMoodleInfo>>> SearchMoodles(SearchBase dto)
    {
        _logger.LogCallInfo();
        // ensure they are a valid user.
        User user = await DbContext.Users.SingleOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false);
        if (user is null)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "User not found.").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient, new List<ServerMoodleInfo>());
        }

        // Start with a base query without including sub-dependencies initially
        IQueryable<MoodleStatus> moodlesQuery = DbContext.Moodles.AsQueryable();

        // 1. Apply title or author / title filters
        if (!string.IsNullOrEmpty(dto.Input))
        {
            string searchStringLower = dto.Input.ToLower(CultureInfo.InvariantCulture);
            moodlesQuery = moodlesQuery.Where(p =>
                p.Author.ToLower().Contains(searchStringLower) ||
                p.Title.ToLower().Contains(searchStringLower));
        }

        // 2. Apply tag filters (only if tags are provided)
        if (dto.Tags != null && dto.Tags.Any())
        {
            // Include the MoodleKeywords for tag filtering (Only include here if we know we are using them)
            moodlesQuery = moodlesQuery.Include(p => p.MoodleKeywords).AsSplitQuery()
                .Where(p => p.MoodleKeywords.Any(t => dto.Tags.Contains(t.KeywordWord)));
        }

        // 3. Apply sorting
        moodlesQuery = dto.Filter is HubFilter.Likes
            ? (dto.Order is HubDirection.Ascending
                ? moodlesQuery.OrderBy(p => p.LikesMoodles.Count)
                : moodlesQuery.OrderByDescending(p => p.LikesMoodles.Count))
            : (dto.Order is HubDirection.Ascending
                ? moodlesQuery.OrderBy(p => p.TimePublished)
                : moodlesQuery.OrderByDescending(p => p.TimePublished));


        // 4. Limit results (only run the this moodle keyword include to the first 50.
        List<MoodleStatus> moodles = await moodlesQuery.Take(75).Include(p => p.MoodleKeywords).Include(p => p.LikesMoodles).AsSplitQuery().ToListAsync().ConfigureAwait(false);

        // Check if final result is null or contains null entries
        if (moodles is null || moodles.Any(p => p is null))
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "No moodles found or some moodles are invalid.").ConfigureAwait(false);
            return HubResponseBuilder.Yippee(new List<ServerMoodleInfo>());
        }


        // Convert to ServerMoodleInfo
        List<ServerMoodleInfo> result = moodles.Select(p => new ServerMoodleInfo
        {
            Likes = p.LikeCount,
            HasLikedMoodle = p.LikesMoodles.Any(likes => string.Equals(likes.UserUID, user.UID, StringComparison.Ordinal)),
            Author = p.Author,
            Tags = p.MoodleKeywords.Select(t => t.KeywordWord).ToHashSet(StringComparer.OrdinalIgnoreCase),
            MoodleStatus = p.ToStatusInfo(),
        }).ToList();

        return HubResponseBuilder.Yippee(result);
    }
}

