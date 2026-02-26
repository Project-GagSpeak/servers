using GagspeakAPI.Data;
using GagspeakAPI.Network;
using GagspeakServer.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GagspeakServer.Hubs;
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
            KinksterPair pairList = new(new UserData(p.Key, p.Value.Alias, p.Value.Tier, p.Value.Created),
                p.Value.OwnPerms.ToApi(),
                p.Value.OwnAccess.ToApi(),
                p.Value.OtherGlobals.ToApi(),
                p.Value.OtherHcState.ToApi(),
                p.Value.OtherPerms.ToApi(),
                p.Value.OtherAccess.ToApi(),
                p.Value.PairInitAt);
            return pairList;
        }).ToList();
    }

    [Authorize(Policy = "Identified")]
    public async Task<ActiveRequests> UserGetActiveRequests()
    {
        // fetch all the pair requests with the UserUid in either the UserUID or OtherUserUID
        List<KinksterRequest> pairRequests = await DbContext.PairRequests.AsNoTracking()
            .Where(k => k.UserUID == UserUID || k.OtherUserUID == UserUID)
            .Select(r => r.ToApi())
            .ToListAsync()
            .ConfigureAwait(false);

        List<CollarRequest> collarRequests = await DbContext.CollarRequests.AsNoTracking()
            .Where(k => k.UserUID == UserUID || k.OtherUserUID == UserUID)
            .Select(r => r.ToApiCollarRequest())
            .ToListAsync()
            .ConfigureAwait(false);
        return new ActiveRequests(pairRequests, collarRequests);
    }

    [Authorize(Policy = "Identified")]
    public async Task<KinkPlateFull> UserGetKinkPlate(KinksterBase user)
    {
        // If requested profile matches the caller, return the full profile always.
        if (string.Equals(user.User.UID, UserUID, StringComparison.Ordinal))
        {
            var ownProfile = await DbContext.ProfileData.AsNoTracking().SingleAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
            return new KinkPlateFull(user.User, ownProfile.FromProfileData(), ownProfile.Base64ProfilePic);
        }

        // Obtain the auth to know if they are allowed to view the profile to begin with, and if the caller is legit.
        if (await DbContext.Auth.Include(a => a.AccountRep).AsNoTracking().SingleOrDefaultAsync(a => a.UserUID == UserUID).ConfigureAwait(false) is not { } auth)
            return new KinkPlateFull(user.User, new KinkPlateContent(), string.Empty);

        // If the caller has bad reputation for profile viewing abuse, return blank profile.
        if (!auth.AccountRep.ProfileViewing)
            return new KinkPlateFull(user.User, new KinkPlateContent() { Description = "Your Reputation prevents you from viewing KinkPlates." }, string.Empty);

        // Profile is valid so get the full profile data.
        var data = await DbContext.ProfileData.AsNoTracking()
            .Include(p => p.CollarData)
            .ThenInclude(c => c.Owners)
            .SingleAsync(u => u.UserUID == user.User.UID)
            .ConfigureAwait(false);
        var content = data.FromProfileData();

        // Get the pairs of the context caller for the IsPublic check.
        if (!data.ProfileIsPublic)
        {
            var callerPairs = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            if (!callerPairs.Contains(user.User.UID, StringComparer.Ordinal))
                return new KinkPlateFull(user.User, content with { Description = "Profile Pic is hidden as they have not allowed public plates!" }, string.Empty);
        }

        if (data.FlaggedForReport)
            return new KinkPlateFull(user.User, content with { Description = "Profile is pending review from CK after being reported" }, string.Empty);

        // Otherwise return the complete profile. 
        content.CollarWriting = data.CollarData.Writing;
        content.CollarOwners = data.CollarData.Owners.Select(o => o.OwnerUID).ToList();
        return new KinkPlateFull(user.User, content, data.Base64ProfilePic);
    }
}

