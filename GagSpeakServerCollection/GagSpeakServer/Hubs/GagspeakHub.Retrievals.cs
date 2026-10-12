using GagspeakAPI.Chat;
using GagspeakAPI.Connection;
using GagspeakAPI.Data;
using GagspeakAPI.Network;
using GagspeakAPI.Profiles;
using GagspeakAPI.User;
using GagspeakServer.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GagspeakServer.Hubs;
public partial class GagspeakHub
{
    [Authorize(Policy = "Identified")]
    public async Task<List<OnlineKinkster>> GetOnlineKinksters()
    {
        var allConnections = await SendOnlineToKinksterPairs().ConfigureAwait(false);
        // filter for the online users.
        var onlineIdents = await GetOnlineUsers(allConnections).ConfigureAwait(false);
        var onlineUids = onlineIdents.Keys.ToList();
        // Get the user entries for these onlineUsers.
        var onlineUsers = await DbContext.Users.AsNoTracking().Where(u => onlineUids.Contains(u.UID)).ToListAsync().ConfigureAwait(false);
        var onlineDto = onlineUsers.Select(u => new OnlineKinkster(u.ToUserData(), onlineIdents[u.UID])).ToList();
        return onlineDto;
    }

    [Authorize(Policy = "Identified")]
    public async Task<List<KinksterPair>> GetAllKinksterPairs()
    {
        var pairs = await GetAllPairInfo(UserUID).ConfigureAwait(false);
        // Convert the UserInfo objects returned into UserPair DTO's for transit.
        return pairs.Select(p =>
        {
            var pairDto = new KinksterPair(
                p.Value.UserData,
                p.Value.OwnPerms.ToApi(),
                p.Value.OwnAccess.ToApi(),
                p.Value.OtherGlobals.ToApi(),
                p.Value.OtherHcState.ToApi(),
                p.Value.OtherPerms.ToApi(),
                p.Value.OtherAccess.ToApi(),
                p.Value.PairInitAt,
                p.Value.PairTempAccepter);
            return pairDto;
        }).ToList();
    }

    [Authorize(Policy = "Identified")]
    public async Task<ActiveRequests> GetRequests()
    {
        // fetch all the pair requests with the UserUid in either the UserUID or OtherUserUID
        List<KinksterRequest> pairRequests = await DbContext.PairRequests.AsNoTracking()
            .Where(k => k.UserUID == UserUID || k.OtherUserUID == UserUID)
            .Select(r => r.ToApi())
            .ToListAsync().ConfigureAwait(false);

        List<CollarRequest> collarRequests = await DbContext.CollarRequests.AsNoTracking()
            .Where(k => k.UserUID == UserUID || k.OtherUserUID == UserUID)
            .Select(r => r.ToApiCollarRequest())
            .ToListAsync().ConfigureAwait(false);
        return new ActiveRequests(pairRequests, collarRequests);
    }

    [Authorize(Policy = "Identified")]
    public async Task<ChatHistoryResult> GetChatHistory(ChatHistoryRequest dto)
    {
        var historyResult = new Dictionary<ChatlogId, List<ChatlogMessage>>();
        if (dto.FetchGlobal)
            historyResult[ChatlogId.GlobalChat] = _chatService.GetGlobalChatHistory(200).ToList();
        // If we request DMs, append them as well.
        if (dto.DMs.Count > 0)
        {
            var validPairs = await GetAllPairedUsers().ConfigureAwait(false);
            var limit = Math.Min(dto.total, 100);
            foreach (var user in dto.DMs)
            {
                if (!validPairs.Contains(user.UID, StringComparer.Ordinal))
                    continue;
                // Get the distinct DM chat ID.
                var chatId = string.CompareOrdinal(UserUID, user.UID) < 0 ? $"{UserUID}-{user.UID}" : $"{user.UID}-{UserUID}";
                var chatlogId = new ChatlogId { Kind = GsChatKind.Direct, ChatId = chatId };
                historyResult[chatlogId] = _chatService.GetPrivateChatHistory(chatlogId, limit).ToList();
            }
        }
        return new ChatHistoryResult(historyResult);
    }

    [Authorize(Policy = "Identified")]
    public async Task<KinkPlateFull> GetKinkplate(UserDto user)
    {
        // If requested profile matches the caller, return the full profile always.
        if (string.Equals(user.User.UID, UserUID, StringComparison.Ordinal))
        {
            var ownProfile = await DbContext.UserProfileData.AsNoTracking()
                .Include(p => p.CollarData).ThenInclude(c => c.Owners)
                .AsSplitQuery()
                .SingleAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
            return new KinkPlateFull(user.User, ownProfile.FromProfileData(), ownProfile.Base64ProfilePic);
        }

        // Obtain the auth to know if they are allowed to view the profile to begin with, and if the caller is legit.
        if (await DbContext.Auth.Include(a => a.AccountRep).AsNoTracking().SingleOrDefaultAsync(a => a.UserUID == UserUID).ConfigureAwait(false) is not { } auth)
            return new KinkPlateFull(user.User, new KinkPlateContent(), string.Empty);

        // If the caller has bad reputation for profile viewing abuse, return blank profile.
        if (!auth.AccountRep.ProfileViewing)
            return new KinkPlateFull(user.User, new KinkPlateContent() { Description = "Your Reputation prevents you from viewing KinkPlates." }, string.Empty);

        // Profile is valid so get the full profile data.
        var data = await DbContext.UserProfileData.AsNoTracking()
            .Include(p => p.CollarData)
            .ThenInclude(c => c.Owners)
            .AsSplitQuery()
            .SingleOrDefaultAsync(u => u.UserUID == user.User.UID)
            .ConfigureAwait(false);
        var content = data.FromProfileData();

        if (data.FlaggedForReport)
            return new KinkPlateFull(user.User, content with { Description = "Profile is pending review from CK after being reported" }, string.Empty);

        if (data is not null)
        {
            content.CollarWriting = data.CollarData.Writing;
            content.CollarOwners = data.CollarData.Owners.Select(o => o.OwnerUID).ToList();
        }
        return new KinkPlateFull(user.User, content, data.Base64ProfilePic);
    }

    /// <summary>
    ///   Retrieves the Profile the requested users.
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task<List<KinkPlateFull>> GetKinkplates(UserListDto dto)
    {
        // Ensure we actually have requests to begin with.
        if (dto.Users.Count is 0)
            return [];
        // Obtain the distinct users to get the profile information for.
        var targets = dto.Users.Select(u => u.UID).Distinct(StringComparer.Ordinal).ToList();
        // Obtain the auth to know if they are allowed to view the profile to begin with, and if the caller is legit.
        if (await DbContext.Auth.Include(a => a.AccountRep).AsNoTracking().SingleOrDefaultAsync(a => a.UserUID == UserUID).ConfigureAwait(false) is not { } callerAuth)
            return [];
        // If they have a bad rep, then return empty profiles for all of them.
        if (!callerAuth.AccountRep.ProfileViewing)
            return [];

        // Bulk-Fetch using AsSplitQuery to prevent Cartesian explosion with the Owners array
        var fetchedProfiles = await DbContext.UserProfileData.AsNoTracking()
            .Include(up => up.User).Include(up => up.CollarData).ThenInclude(c => c.Owners)
            .AsSplitQuery()
            .Where(u => targets.Contains(u.UserUID))
            .ToListAsync().ConfigureAwait(false);
        if (fetchedProfiles.Count is 0)
            return [];
        // Compose the list
        var results = new List<KinkPlateFull>(fetchedProfiles.Count);
        foreach (var retProfile in fetchedProfiles)
        {
            var content = retProfile.FromProfileData();
            // Map the collar data safely
            content.CollarWriting = retProfile.CollarData?.Writing ?? string.Empty;
            content.CollarOwners = retProfile.CollarData?.Owners?.Select(o => o.OwnerUID).ToList() ?? [];
            // Ret full profile for self.
            if (string.Equals(retProfile.UserUID, UserUID, StringComparison.Ordinal))
                results.Add(new KinkPlateFull(retProfile.User.ToUserData(), retProfile.FromProfileData(), retProfile.Base64ProfilePic));
            else
            {
                var image = retProfile.FlaggedForReport ? string.Empty : retProfile.Base64ProfilePic;
                var safeContent = retProfile.FlaggedForReport ? content with { Description = "Profile is pending review from CK after being reported" } : content;
                results.Add(new KinkPlateFull(retProfile.User.ToUserData(), safeContent, image));
            }
        }
        return results;
    }
}

