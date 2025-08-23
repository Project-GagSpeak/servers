using GagspeakAPI.Data;
using GagspeakAPI.Network;
using GagspeakServer.Utils;
using GagspeakShared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GagspeakServer.Hubs;
#nullable enable

public partial class GagspeakHub
{
    [Authorize(Policy = "Identified")]
    public async Task<List<OnlineKinkster>> UserGetOnlinePairs()
    {
        _logger.LogCallInfo();

        List<string> allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
        Dictionary<string, string> pairs = await GetOnlineUsers(allPairedUsers).ConfigureAwait(false);
        // send that you are online to all connected online pairs of the client caller.
        await SendOnlineToAllPairedUsers().ConfigureAwait(false);
        // then, return back to the client caller the list of all users that are online in their client pairs.
        return pairs.Select(p => new OnlineKinkster(new(p.Key), p.Value)).ToList();
    }

    [Authorize(Policy = "Identified")]
    public async Task<List<KinksterPair>> UserGetPairedClients()
    {
        // fetch all the pair information of the client caller
        Dictionary<string, UserInfo> pairs = await GetAllPairInfo(UserUID).ConfigureAwait(false);

        // return the list of UserPair DTO's containing the paired clients of the client caller
        return pairs.Select(p =>
        {
            KinksterPair pairList = new(new UserData(p.Key, p.Value.Verified, p.Value.Alias, p.Value.SupporterTier, p.Value.createdDate),
                p.Value.ownPairPermissions.ToApiKinksterPerms(),
                p.Value.ownPairPermissionAccess.ToApiKinksterEditAccess(),
                p.Value.otherGlobalPerms.ToApiGlobalPerms(),
                p.Value.otherHardcoreState.ToApiHardcoreState(),
                p.Value.otherPairPermissions.ToApiKinksterPerms(),
                p.Value.otherPairPermissionAccess.ToApiKinksterEditAccess());
            return pairList;
        }).ToList();
    }

    [Authorize(Policy = "Identified")]
    public async Task<ActiveRequests> UserGetActiveRequests()
    {
        // fetch all the pair requests with the UserUid in either the UserUID or OtherUserUID
        List<KinksterPairRequest> pairRequests = await DbContext.KinksterPairRequests.AsNoTracking()
            .Where(k => k.UserUID == UserUID || k.OtherUserUID == UserUID)
            .Select(r => r.ToApiPairRequest())
            .ToListAsync()
            .ConfigureAwait(false);

        List<CollarOwnershipRequest> collarRequests = await DbContext.CollarRequests.AsNoTracking()
            .Where(k => k.UserUID == UserUID || k.OtherUserUID == UserUID)
            .Select(r => r.ToApiCollarRequest())
            .ToListAsync()
            .ConfigureAwait(false);
        return new ActiveRequests(pairRequests, collarRequests);
    }

    [Authorize(Policy = "Identified")]
    public async Task<KinkPlateFull> UserGetKinkPlate(KinksterBase user)
    {
        List<string> callerPairs = await GetAllPairedUnpausedUsers().ConfigureAwait(false);

        UserProfileData? data = await DbContext.UserProfileData.AsNoTracking()
            .Include(p => p.CollarData).ThenInclude(c => c.Owners)
            .SingleOrDefaultAsync(u => u.UserUID == user.User.UID)
            .ConfigureAwait(false);

        // Return blank profile if it does not exist.
        if (data is null)
            return new KinkPlateFull(user.User, new KinkPlateContent(), string.Empty);

        // If not in the callers KinksterPair list and not set to public, return nonPublic profile.
        // If requested User Profile is not in list of pairs, and is not self, return blank profile update.
        if (!callerPairs.Contains(user.User.UID, StringComparer.Ordinal) && !string.Equals(user.User.UID, UserUID, StringComparison.Ordinal) && !data.ProfileIsPublic)
            return new KinkPlateFull(user.User, new KinkPlateContent() { Description = "Profile is not Public!" }, string.Empty);

        // If profile is disabled / restricted, return disabled profile.
        if (data.ProfileDisabled)
            return new KinkPlateFull(user.User, new KinkPlateContent() { Disabled = true, Description = "This profile is currently disabled" }, string.Empty);

        // It is valid, so we should grab the collar related data.
        KinkPlateContent content = data.FromProfileData();
        content.CollarWriting = data.CollarData.Writing;
        content.CollarOwners = data.CollarData.Owners.Select(o => o.OwnerUID).ToList();
        return new KinkPlateFull(user.User, content, data.Base64ProfilePic);
    }
}

