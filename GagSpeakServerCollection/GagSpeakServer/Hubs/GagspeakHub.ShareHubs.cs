using GagspeakAPI.Data;
using GagspeakAPI.Dto.Sharehub;
using GagspeakAPI.Enums;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakShared.Metrics;
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
    public async Task<HubResponse> UploadPattern(SharehubUploadPattern dto)
    {
        _logger.LogCallInfo();

        // if the guid is empty, it's not a valid pattern.
        if (dto.PatternInfo.Identifier == Guid.Empty)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // Uploader must have valid auth.
        if (await DbContext.Auth.Include(a => a.User).Include(a => a.AccountRep).SingleOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false) is not { } auth)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // A Pattern of the same GUID cannot exist, or be a pattern of the same name.
        if (await DbContext.Patterns.AsNoTracking().AnyAsync(p => p.Identifier == dto.PatternInfo.Identifier || p.Name == dto.PatternInfo.Label).ConfigureAwait(false))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.DuplicateEntry);

        // Must have remaining upload allowances.
        if (auth.AccountRep.UploadAllowances <= 0)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.UploadLimitExceeded);

        // Decrement their upload allowances and begin the upload.
        auth.AccountRep.UploadAllowances = Math.Max(0, auth.AccountRep.UploadAllowances - 1);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        /////////////// Step 1: Check and add tags //////////////////
        var uploadTagsLower = dto.PatternInfo.Tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.ToLowerInvariant()).ToList();
        // Get all existing tags from the database
        var existingTags = await DbContext.Keywords.AsNoTracking().Where(t => uploadTagsLower.Contains(t.Word)).ToListAsync().ConfigureAwait(false);
        // Get the new tags that are not in the database
        var newTags = uploadTagsLower.Except(existingTags.Select(t => t.Word), StringComparer.Ordinal).ToList();
        // Create and insert the new tags not yet in DB
        foreach (string newTagName in newTags)
        {
            Keyword newTag = new Keyword { Word = newTagName };
            DbContext.Keywords.Add(newTag);
            existingTags.Add(newTag);
        }

        ///////////// Step 2: Create and insert the new Pattern Entry /////////////
        PatternEntry newPatternEntry = new()
        {
            Identifier = dto.PatternInfo.Identifier,
            PublisherUID = UserUID,
            TimePublished = DateTime.UtcNow,
            Name = dto.PatternInfo.Label,
            Description = dto.PatternInfo.Description,
            Author = dto.PatternInfo.Author,
            PatternKeywords = new List<PatternKeyword>(),
            DownloadCount = 0,
            UserPatternLikes = new List<LikesPatterns>(),
            ShouldLoop = dto.PatternInfo.Looping,
            Length = dto.PatternInfo.Length,
            Base64PatternData = dto.PatternDataBase64,
        };
        DbContext.Patterns.Add(newPatternEntry);

        ///////////// Step 3: Create and insert the new Pattern Entry Tags /////////////
        foreach (Keyword tag in existingTags)
        {
            DbContext.PatternKeywords.Add(new PatternKeyword
            {
                PatternEntryId = newPatternEntry.Identifier,
                KeywordWord = tag.Word
            });
        }

        // Save the user with the new uploaded pattern.
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterUploadedPatterns);
        _metrics.IncGauge(MetricsAPI.GaugeShareHubPatterns);
        return HubResponseBuilder.Yippee();
    }

    public async Task<HubResponse> UploadLociStatus(SharehubUploadLociStatus dto)
    {
        _logger.LogCallInfo();

        // Ensure valid Moodle ID.
        if (dto.Status.GUID == Guid.Empty)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // Ensure valid uploader.
        if (await DbContext.Users.SingleOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false) is not { } user)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Ensure Moodle does not already exist.
        if (await DbContext.LociStatuses.AsNoTracking().AnyAsync(p => p.Identifier == dto.Status.GUID).ConfigureAwait(false))
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "A Loci Status with the same ID already exists.").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.DuplicateEntry);
        }

        /////////////// Step 1: Check and add tags //////////////////
        var uploadTagsLower = dto.Tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.ToLowerInvariant()).ToList();
        // Get all existing tags from the database
        var existingTags = await DbContext.Keywords.AsNoTracking().Where(t => uploadTagsLower.Contains(t.Word)).ToListAsync().ConfigureAwait(false);
        // Get the new tags that are not in the database
        var newTags = uploadTagsLower.Except(existingTags.Select(t => t.Word), StringComparer.Ordinal).ToList();
        // Create and insert the new tags not yet in DB
        foreach (string newTagName in newTags)
        {
            Keyword newTag = new Keyword { Word = newTagName };
            DbContext.Keywords.Add(newTag);
            existingTags.Add(newTag);
        }

        ///////////// Step 2: Create and insert the new Pattern Entry /////////////
        var totalTime = dto.Status.ExpireTicks == -1 
            ? TimeSpan.Zero : TimeSpan.FromMilliseconds(dto.Status.ExpireTicks);
        LociStatus newMoodleEntry = new()
        {
            Identifier = dto.Status.GUID,
            PublisherUID = UserUID,
            TimePublished = DateTime.UtcNow,
            Author = dto.AuthorName,
            LociKeywords = new List<LociKeyword>(),
            // moodle information.
            IconID = dto.Status.IconID,
            Title = dto.Status.Title,
            Description = dto.Status.Description,
            CustomFXPath = dto.Status.CustomVFXPath,
            Type = dto.Status.Type,
            Stacks = dto.Status.Stacks,
            StackSteps = dto.Status.StackSteps,
            StackToChain = dto.Status.StackToChain,
            Modifiers = dto.Status.Modifiers,
            ChainedGUID = dto.Status.ChainedGUID,
            ChainType = dto.Status.ChainType,
            ChainTrigger = dto.Status.ChainTrigger,
            Days = totalTime.Days,
            Hours = totalTime.Hours,
            Minutes = totalTime.Minutes,
            Seconds = totalTime.Seconds,
            Permanent = dto.Status.ExpireTicks == -1,
        };
        DbContext.LociStatuses.Add(newMoodleEntry);

        ///////////// Step 3: Create and insert the new Pattern Entry Tags /////////////
        foreach (Keyword tag in existingTags)
        {
            DbContext.LociKeywords.Add(new LociKeyword
            {
                MoodleStatusId = newMoodleEntry.Identifier,
                KeywordWord = tag.Word
            });
        }

        // Save the user with the new upload log
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterUploadedMoodles);
        _metrics.IncGauge(MetricsAPI.GaugeShareHubMoodles);
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
        _metrics.IncCounter(MetricsAPI.CounterPatternDownloads);
        return HubResponseBuilder.Yippee(pattern.Base64PatternData);
    }

    public async Task<HubResponse> LikePattern(Guid patternId)
    {
        _logger.LogCallInfo();
        if (await DbContext.Users.SingleOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false) is not { } user) 
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);
        // Locate the pattern in the database by its GUID.
        if (await DbContext.Patterns.SingleOrDefaultAsync(p => p.Identifier == patternId).ConfigureAwait(false) is not { } pattern)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.ShareHubEntryNotFound);

        // Check if the user has already liked this pattern
        if (await DbContext.PatternLikes.SingleOrDefaultAsync(upl => upl.PatternEntryId == patternId && upl.UserUID == user.UID).ConfigureAwait(false) is { } liked)        
        {
            DbContext.PatternLikes.Remove(liked);
            _metrics.DecGauge(MetricsAPI.GaugePatternLikes);
        }
        else
        {
            // User has not liked this pattern, so add a new like
            var userPatternLike = new LikesPatterns()
            {
                UserUID = user.UID,
                User = user,
                PatternEntryId = patternId,
                PatternEntry = pattern
            };
            DbContext.PatternLikes.Add(userPatternLike);
            _metrics.IncGauge(MetricsAPI.GaugePatternLikes);
        }
        // Save changes to the database
        await DbContext.SaveChangesAsync().ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    public async Task<HubResponse> LikeLociStatus(Guid statusId)
    {
        _logger.LogCallInfo();
        // Get the current user
        if (await DbContext.Users.SingleOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false) is not { } user)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        // Locate the pattern in the database by its GUID.
        if (await DbContext.LociStatuses.SingleOrDefaultAsync(p => p.Identifier == statusId).ConfigureAwait(false) is not { } status)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.ShareHubEntryNotFound);

        // Check if the user has already liked this pattern
        if (await DbContext.LociStatusLikes.SingleOrDefaultAsync(upl => upl.LociStatusId == statusId && upl.UserUID == user.UID).ConfigureAwait(false) is { } liked)
        {
            DbContext.LociStatusLikes.Remove(liked);
            _metrics.DecGauge(MetricsAPI.GaugeLociLikes);
        }
        else
        {
            // User has not liked this pattern, so add a new like
            var userPatternLike = new LikesLoci()
            {
                UserUID = user.UID,
                User = user,
                LociStatusId = statusId,
                LociStatus = status
            };
            DbContext.LociStatusLikes.Add(userPatternLike);
            _metrics.IncGauge(MetricsAPI.GaugeLociLikes);
        }

        // Save changes to the database
        await DbContext.SaveChangesAsync().ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }
    public async Task<HubResponse> DelistPattern(Guid patternId)
    {
        _logger.LogCallInfo();
        if (await DbContext.Users.SingleOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false) is not { } user)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);
        
        // Find the pattern by GUID and ensure it belongs to the user
        if (await DbContext.Patterns.SingleOrDefaultAsync(p => p.Identifier == patternId && p.PublisherUID == user.UID).ConfigureAwait(false) is not { } pattern)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "Pattern not found or it's not yours.").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPublisher);
        }

        // Find and remove related PatternEntryTags
        List<PatternKeyword> patternEntryTags = await DbContext.PatternKeywords.Where(pet => pet.PatternEntryId == patternId).ToListAsync().ConfigureAwait(false);
        DbContext.PatternKeywords.RemoveRange(patternEntryTags);
        DbContext.Patterns.Remove(pattern);

        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        _metrics.DecGauge(MetricsAPI.GaugeShareHubPatterns);
        return HubResponseBuilder.Yippee();
    }

    public async Task<HubResponse> DelistLociStatus(Guid statusId)
    {
        _logger.LogCallInfo();
        if (await DbContext.Users.SingleOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false) is not { } user)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient);

        if (await DbContext.LociStatuses.SingleOrDefaultAsync(p => p.Identifier == statusId && p.PublisherUID == user.UID).ConfigureAwait(false) is not { } status)
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "Status not found or it's not yours.").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotPublisher);
        }

        // Find and remove related keywords mapping from the Keywords table and the MoodleStautsId
        var lociKeywords = await DbContext.LociKeywords.Where(pet => pet.MoodleStatusId == statusId).ToListAsync().ConfigureAwait(false);
        DbContext.LociKeywords.RemoveRange(lociKeywords);
        DbContext.LociStatuses.Remove(status);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        _metrics.DecGauge(MetricsAPI.GaugeShareHubMoodles);
        return HubResponseBuilder.Yippee();
    }

    // Possibly revise this later or something
    public async Task<HubResponse<List<SharehubPattern>>> SearchPatterns(SearchPattern dto)
    {
        _logger.LogCallInfo();
        // Only valid users can use the sharehub.
        if (await DbContext.Users.SingleOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false) is not { } user)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient, new List<SharehubPattern>());

        // We need to now perform a ran query using .AsNoTracking
        // This query must include all nessisary searchable indexes for efficient lookup.
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
            case HubSortBy.DatePosted:
                patternsQuery = dto.Order is SortDirection.Ascending
                    ? patternsQuery.OrderBy(p => p.TimePublished)
                    : patternsQuery.OrderByDescending(p => p.TimePublished);
                break;
            case HubSortBy.Downloads:
                patternsQuery = dto.Order is SortDirection.Ascending
                    ? patternsQuery.OrderBy(p => p.DownloadCount)
                    : patternsQuery.OrderByDescending(p => p.DownloadCount);
                break;
            case HubSortBy.Likes:
                patternsQuery = dto.Order is SortDirection.Ascending
                    ? patternsQuery.OrderBy(p => p.UserPatternLikes.Count)
                    : patternsQuery.OrderByDescending(p => p.UserPatternLikes.Count);
                break;
        }

        // limit the results to 30 patterns.
        var patterns = await patternsQuery.Take(30).Include(p => p.PatternKeywords).Include(p => p.UserPatternLikes).AsSplitQuery().ToListAsync().ConfigureAwait(false);

        // Check if patterns is null or contains null entries
        if (patterns is null || patterns.Any(p => p is null))
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "No patterns found or some patterns are invalid.").ConfigureAwait(false);
            return HubResponseBuilder.Yippee(new List<SharehubPattern>());
        }

        // Convert to ServerPatternInfo
        var result = patterns.Select(p => new SharehubPattern
        {
            Version = p.Version,
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
        _metrics.IncCounter(MetricsAPI.CounterShareHubSearches);
        return HubResponseBuilder.Yippee(result);
    }

    // Possibly revise this later or something
    public async Task<HubResponse<List<SharehubLociStatus>>> SearchLociData(SearchBase dto)
    {
        _logger.LogCallInfo();
        if (await DbContext.Users.SingleOrDefaultAsync(u => u.UID == UserUID).ConfigureAwait(false) is not { } user)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.InvalidRecipient, new List<SharehubLociStatus>());

        // We need to now perform a ran query using .AsNoTracking
        // This query must include all nessisary searchable indexes for efficient lookup.
        IQueryable<LociStatus> searchQuery = DbContext.LociStatuses.AsQueryable();

        // 1. Apply title or author / title filters
        if (!string.IsNullOrEmpty(dto.Input))
        {
            string searchStringLower = dto.Input.ToLower(CultureInfo.InvariantCulture);
            searchQuery = searchQuery.Where(p =>
                p.Author.ToLower().Contains(searchStringLower) ||
                p.Title.ToLower().Contains(searchStringLower));
        }

        // 2. Apply tag filters (only if tags are provided)
        if (dto.Tags != null && dto.Tags.Any())
        {
            // Include the LociKeywords for tag filtering (Only include here if we know we are using them)
            searchQuery = searchQuery.Include(p => p.LociKeywords).AsSplitQuery()
                .Where(p => p.LociKeywords.Any(t => dto.Tags.Contains(t.KeywordWord)));
        }

        // 3. Apply sorting
        searchQuery = dto.Filter is HubSortBy.Likes
            ? (dto.Order is SortDirection.Ascending
                ? searchQuery.OrderBy(p => p.LikesLoci.Count)
                : searchQuery.OrderByDescending(p => p.LikesLoci.Count))
            : (dto.Order is SortDirection.Ascending
                ? searchQuery.OrderBy(p => p.TimePublished)
                : searchQuery.OrderByDescending(p => p.TimePublished));


        // 4. Limit results (only run the this keyword include to the first 50.
        var matches = await searchQuery.Take(75).Include(p => p.LociKeywords).Include(p => p.LikesLoci).AsSplitQuery().ToListAsync().ConfigureAwait(false);

        // Check if final result is null or contains null entries
        if (matches is null || matches.Exists(p => p is null))
        {
            await Clients.Caller.Callback_ServerMessage(MessageSeverity.Warning, "No statuses found or some were invalid.").ConfigureAwait(false);
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData, new List<SharehubLociStatus>());
        }

        // Convert to ServerStatus
        var result = matches.Select(p => new SharehubLociStatus()
        {
            Author = p.Author,
            Tags = p.LociKeywords.Select(t => t.KeywordWord).ToHashSet(StringComparer.OrdinalIgnoreCase),
            Status = p.ToStruct(),
            Likes = p.LikeCount,
            HasLiked = p.LikesLoci.Any(likes => string.Equals(likes.UserUID, user.UID, StringComparison.Ordinal)),
        }).ToList();

        _metrics.IncCounter(MetricsAPI.CounterShareHubSearches);
        return HubResponseBuilder.Yippee(result);
    }
}

