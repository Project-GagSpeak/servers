using GagspeakAPI.Data;
using GagspeakAPI.Data.Enum;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Dto.Connection;
using GagspeakAPI.Dto.Permissions;
using GagspeakAPI.Dto.UserPair;
using GagspeakServer.Utils;
using GagspeakShared.Metrics;
using GagspeakShared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using GagspeakAPI.Dto.Toybox;
using GagspeakAPI.Data.Character;
using Microsoft.IdentityModel.Tokens;

namespace GagspeakServer.Hubs;

/// <summary> 
/// This partial class of the GagSpeakHub contains all the user related methods for pushing data.
/// </summary>
public partial class GagspeakHub
{
    private const string None = "None";
    private const string OwnerPadlock = "OwnerPadlock";
    private const string OwnerTimerPadlock = "OwnerTimerPadlock";


    /// <summary> 
    /// Called by a connected client that desires to push the latest updates for their character COMBINED data
    /// </summary>
    [Authorize(Policy = "Identified")]
    public async Task UserPushData(UserCharaCompositeDataMessageDto dto)
    {
        _logger.LogCallInfo();

        // fetch the recipient UID list from the recipient list of the dto
        var recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        // check to see if all the recipients are cached within the cache service. If not, then cache them.
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            // fetch all the paired users of the client caller
            var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            // see which of the paired users are in the recipient list, and cache them.
            recipientUids = allPairedUsers.Where(f => recipientUids.Contains(f, StringComparer.Ordinal)).ToList();
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }

        _logger.LogCallInfo(GagspeakHubLogger.Args(recipientUids.Count));
        await Clients.Users(recipientUids).Client_UserReceiveCharacterDataComposite(
            new OnlineUserCompositeDataDto(new UserData(UserUID), dto.CompositeData, DataUpdateKind.FullDataUpdate)).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterUserPushDataComposite);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataCompositeTo, recipientUids.Count);
    }

    /// <summary> Called by a connected client to push own latest IPC Data to other paired clients. </summary>
    public async Task UserPushDataIpc(UserCharaIpcDataMessageDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        var recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            recipientUids = allPairedUsers.Where(f => recipientUids.Contains(f, StringComparer.Ordinal)).ToList();
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }

        _logger.LogCallInfo(GagspeakHubLogger.Args(recipientUids.Count));
        // fetch the new Dto to send (with Client Caller's userData as the attached User) to other paired clients.
        await Clients.Users(recipientUids).Client_UserReceiveOtherDataIpc(new OnlineUserCharaIpcDataDto(new UserData(UserUID), dto.IPCData, dto.UpdateKind)).ConfigureAwait(false);

        // update metrics.
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataIpc);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataIpcTo, recipientUids.Count);
    }

    /// <summary> 
    /// Called by a connected client that desires to push the latest updates for their character's APPEARANCE Data
    /// </summary>
    public async Task UserPushDataAppearance(UserCharaAppearanceDataMessageDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // verify we have our recipients cached.
        var recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            recipientUids = allPairedUsers.Where(f => recipientUids.Contains(f, StringComparer.Ordinal)).ToList();
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }

        // Grab our Appearance from the DB
        var curGagData = await DbContext.UserAppearanceData.FirstOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
        if (curGagData == null) throw new Exception("Cannot update other Pair, User does not have appearance data!");

        // Perform security check on the UpdateKind, to make sure client caller is not exploiting the system.
        switch (dto.UpdateKind)
        {
            // Throw if gag features not allowed OR slot is occupied. Otherwise, update the respective appearance data.
            case DataUpdateKind.AppearanceGagAppliedLayerOne:
                curGagData.SlotOneGagType = dto.AppearanceData.GagSlots[0].GagType;
                break;
            case DataUpdateKind.AppearanceGagAppliedLayerTwo:
                curGagData.SlotTwoGagType = dto.AppearanceData.GagSlots[1].GagType;
                break;
            case DataUpdateKind.AppearanceGagAppliedLayerThree:
                curGagData.SlotThreeGagType = dto.AppearanceData.GagSlots[2].GagType;
                break;
            // Handle lock logic. Throw if lock already present, or if padlock is OwnerPadlock type and OwnerLocks are not allowed.
            case DataUpdateKind.AppearanceGagLockedLayerOne:
                curGagData.SlotOneGagPadlock = dto.AppearanceData.GagSlots[0].Padlock;
                curGagData.SlotOneGagPassword = dto.AppearanceData.GagSlots[0].Password;
                curGagData.SlotOneGagTimer = dto.AppearanceData.GagSlots[0].Timer;
                curGagData.SlotOneGagAssigner = dto.AppearanceData.GagSlots[0].Assigner;
                break;
            case DataUpdateKind.AppearanceGagLockedLayerTwo:
                curGagData.SlotTwoGagPadlock = dto.AppearanceData.GagSlots[1].Padlock;
                curGagData.SlotTwoGagPassword = dto.AppearanceData.GagSlots[1].Password;
                curGagData.SlotTwoGagTimer = dto.AppearanceData.GagSlots[1].Timer;
                curGagData.SlotTwoGagAssigner = dto.AppearanceData.GagSlots[1].Assigner;
                break;
            case DataUpdateKind.AppearanceGagLockedLayerThree:
                curGagData.SlotThreeGagPadlock = dto.AppearanceData.GagSlots[2].Padlock;
                curGagData.SlotThreeGagPassword = dto.AppearanceData.GagSlots[2].Password;
                curGagData.SlotThreeGagTimer = dto.AppearanceData.GagSlots[2].Timer;
                curGagData.SlotThreeGagAssigner = dto.AppearanceData.GagSlots[2].Assigner;
                break;
            // for unlocking, throw if GagFeatures not allowed, the slot is not already locked, or if unlock validation is not met.
            case DataUpdateKind.AppearanceGagUnlockedLayerOne:
                curGagData.SlotOneGagPadlock = None;
                curGagData.SlotOneGagPassword = string.Empty;
                curGagData.SlotOneGagTimer = DateTimeOffset.UtcNow;
                curGagData.SlotOneGagAssigner = string.Empty;
                break;
            case DataUpdateKind.AppearanceGagUnlockedLayerTwo:
                curGagData.SlotTwoGagPadlock = None;
                curGagData.SlotTwoGagPassword = string.Empty;
                curGagData.SlotTwoGagTimer = DateTimeOffset.UtcNow;
                curGagData.SlotTwoGagAssigner = string.Empty;
                break;
            case DataUpdateKind.AppearanceGagUnlockedLayerThree:
                curGagData.SlotThreeGagPadlock = None;
                curGagData.SlotThreeGagPassword = string.Empty;
                curGagData.SlotThreeGagTimer = DateTimeOffset.UtcNow;
                curGagData.SlotThreeGagAssigner = string.Empty;
                break;
            // for removal, throw if gag is locked, or if GagFeatures not allowed.
            case DataUpdateKind.AppearanceGagRemovedLayerOne:
                curGagData.SlotOneGagType = None;
                curGagData.SlotOneGagAssigner = string.Empty;
                break;
            case DataUpdateKind.AppearanceGagRemovedLayerTwo:
                curGagData.SlotTwoGagType = None;
                curGagData.SlotTwoGagAssigner = string.Empty;
                break;
            case DataUpdateKind.AppearanceGagRemovedLayerThree:
                curGagData.SlotThreeGagType = None;
                curGagData.SlotThreeGagAssigner = string.Empty;
                break;
            case DataUpdateKind.Safeword:
                // clear the appearance data for all gags.
                curGagData.SlotOneGagType = None;
                curGagData.SlotOneGagPadlock = None;
                curGagData.SlotOneGagPassword = string.Empty;
                curGagData.SlotOneGagTimer = DateTimeOffset.UtcNow;
                curGagData.SlotOneGagAssigner = string.Empty;
                curGagData.SlotTwoGagType = None;
                curGagData.SlotTwoGagPadlock = None;
                curGagData.SlotTwoGagPassword = string.Empty;
                curGagData.SlotTwoGagTimer = DateTimeOffset.UtcNow;
                curGagData.SlotTwoGagAssigner = string.Empty;
                curGagData.SlotThreeGagType = None;
                curGagData.SlotThreeGagPadlock = None;
                curGagData.SlotThreeGagPassword = string.Empty;
                curGagData.SlotThreeGagTimer = DateTimeOffset.UtcNow;
                curGagData.SlotThreeGagAssigner = string.Empty;
                break;
            default:
                throw new HubException("Invalid UpdateKind for Appearance Data!");
        }

        // update the database with the new appearance data.
        DbContext.UserAppearanceData.Update(curGagData);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // migrate the current appearance data with its changes to a new dto object for sending
        var updatedAppearanceData = new CharacterAppearanceData()
        {
            GagSlots = new GagSlot[]
            {
                new GagSlot
                {
                    GagType = curGagData.SlotOneGagType,
                    Padlock = curGagData.SlotOneGagPadlock,
                    Password = curGagData.SlotOneGagPassword,
                    Timer = curGagData.SlotOneGagTimer,
                    Assigner = curGagData.SlotOneGagAssigner
                },
                new GagSlot
                {
                    GagType = curGagData.SlotTwoGagType,
                    Padlock = curGagData.SlotTwoGagPadlock,
                    Password = curGagData.SlotTwoGagPassword,
                    Timer = curGagData.SlotTwoGagTimer,
                    Assigner = curGagData.SlotTwoGagAssigner
                },
                new GagSlot
                {
                    GagType = curGagData.SlotThreeGagType,
                    Padlock = curGagData.SlotThreeGagPadlock,
                    Password = curGagData.SlotThreeGagPassword,
                    Timer = curGagData.SlotThreeGagTimer,
                    Assigner = curGagData.SlotThreeGagAssigner
                }
            }
        };

        _logger.LogCallInfo(GagspeakHubLogger.Args(recipientUids.Count));
        await Clients.Users(recipientUids).Client_UserReceiveOtherDataAppearance(
            new OnlineUserCharaAppearanceDataDto(new UserData(UserUID), updatedAppearanceData, dto.UpdateKind)).ConfigureAwait(false);
        // push back unlock to ourselves so we can get confirmation that the unlock was successful, and proceed to remove the set if naturally unlocked.
        await Clients.Caller.Client_UserReceiveOwnDataAppearance(
            new OnlineUserCharaAppearanceDataDto(new UserData(UserUID), updatedAppearanceData, dto.UpdateKind)).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterUserPushDataAppearance);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataAppearanceTo, recipientUids.Count);
    }

    /// <summary> 
    /// Called by a connected client that desires to push the latest updates for their character's WARDROBE Data
    /// </summary>
    public async Task UserPushDataWardrobe(UserCharaWardrobeDataMessageDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        var recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            recipientUids = allPairedUsers.Where(f => recipientUids.Contains(f, StringComparer.Ordinal)).ToList();
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }

        // grab our activestatedata
        var userActiveState = await DbContext.UserActiveStateData.FirstOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
        if (userActiveState == null) throw new Exception("Cannot update own userActiveStateData, you somehow does not have active state data!");

        // update it with our activeStateData with the respective changes.
        switch (dto.UpdateKind)
        {
            case DataUpdateKind.WardrobeRestraintOutfitsUpdated:
                break;
            case DataUpdateKind.WardrobeRestraintApplied:
                userActiveState.WardrobeActiveSetName = dto.WardrobeData.ActiveSetName;
                userActiveState.WardrobeActiveSetAssigner = dto.WardrobeData.ActiveSetEnabledBy;
                break;
            case DataUpdateKind.WardrobeRestraintLocked:
                userActiveState.WardrobeActiveSetPadLock = dto.WardrobeData.Padlock;
                userActiveState.WardrobeActiveSetPassword = dto.WardrobeData.Password;
                userActiveState.WardrobeActiveSetLockTime = dto.WardrobeData.Timer;
                userActiveState.WardrobeActiveSetLockAssigner= dto.WardrobeData.Assigner;
                break;
            case DataUpdateKind.WardrobeRestraintUnlocked:
                userActiveState.WardrobeActiveSetPadLock = "None";
                userActiveState.WardrobeActiveSetPassword = string.Empty;
                userActiveState.WardrobeActiveSetLockTime = DateTimeOffset.UtcNow;
                userActiveState.WardrobeActiveSetLockAssigner = string.Empty;
                break;
            case DataUpdateKind.WardrobeRestraintDisabled:
                userActiveState.WardrobeActiveSetName = string.Empty;
                userActiveState.WardrobeActiveSetAssigner = string.Empty;
                break;
            case DataUpdateKind.Safeword:
                // clear all wardrobe related active state data.
                userActiveState.WardrobeActiveSetName = string.Empty;
                userActiveState.WardrobeActiveSetAssigner = string.Empty;
                userActiveState.WardrobeActiveSetPadLock = "None";
                userActiveState.WardrobeActiveSetPassword = string.Empty;
                userActiveState.WardrobeActiveSetLockTime = DateTimeOffset.UtcNow;
                userActiveState.WardrobeActiveSetLockAssigner = string.Empty;
                break;
            default:
                throw new Exception("Invalid UpdateKind for Wardrobe Data!");
        }

        // update the database with the new appearance data.
        DbContext.UserActiveStateData.Update(userActiveState);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogCallInfo(GagspeakHubLogger.Args(recipientUids.Count));
        await Clients.Users(recipientUids).Client_UserReceiveOtherDataWardrobe(
            new OnlineUserCharaWardrobeDataDto(new UserData(UserUID), dto.WardrobeData, dto.UpdateKind)).ConfigureAwait(false);
        // push back unlock to ourselves so we can get confirmation that the unlock was successful, and proceed to remove the set if naturally unlocked.
        await Clients.Caller.Client_UserReceiveOwnDataWardrobe(new OnlineUserCharaWardrobeDataDto(new UserData(UserUID), dto.WardrobeData, dto.UpdateKind)).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterUserPushDataWardrobe);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataWardrobeTo, recipientUids.Count);
    }

    /// <summary> 
    /// Called by a connected client that desires to push the latest updates for their character's ALIAS Data
    /// </summary>
    public async Task UserPushDataAlias(UserCharaAliasDataMessageDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // convert the recipient UID list from the recipient list of the dto
        var recipientUidList = new List<string>() { dto.RecipientUser.UID };
        var recipientUid = dto.RecipientUser.UID;

        // if the recipient is not cached, cache them.
        bool isCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUidList, Context.ConnectionAborted).ConfigureAwait(false);
        if (!isCached)
        {
            var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            recipientUidList = allPairedUsers.Where(f => recipientUidList.Contains(f, StringComparer.Ordinal)).ToList();
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }

        // REVIEW: We can input checks against activestatedata here if we run into concurrency issues.

        // push the notification to the recipient user
        await Clients.User(recipientUid).Client_UserReceiveOtherDataAlias(
            new OnlineUserCharaAliasDataDto(new UserData(UserUID), dto.AliasData, dto.UpdateKind)).ConfigureAwait(false);
        // push the notification to the client caller
        await Clients.Caller.Client_UserReceiveOtherDataAlias(new OnlineUserCharaAliasDataDto(dto.RecipientUser, dto.AliasData, dto.UpdateKind)).ConfigureAwait(false);

        // inc the metrics
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataAlias);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataAliasTo, 2);
    }

    /// <summary> Called by a connected client that desires to push the latest updates for their character's ToyboxData </summary>
    public async Task UserPushDataToybox(UserCharaToyboxDataMessageDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        var recipientUids = dto.Recipients.Select(r => r.UID).ToList();
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
            recipientUids = allPairedUsers.Where(f => recipientUids.Contains(f, StringComparer.Ordinal)).ToList();
            await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
        }

        // REVIEW: We can input checks against activestatedata here if we run into concurrency issues.
        if (dto.UpdateKind == DataUpdateKind.Safeword)
        {
            var userActiveState = await DbContext.UserActiveStateData.FirstOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
            if (userActiveState == null)
            {
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Cannot update own userActiveStateData!").ConfigureAwait(false);
                return;
            }
            userActiveState.ToyboxActivePatternId = Guid.Empty;
            // update the database with the new appearance data.
            DbContext.UserActiveStateData.Update(userActiveState);
        }
        _logger.LogCallInfo(GagspeakHubLogger.Args(recipientUids.Count));
        await Clients.Users(recipientUids).Client_UserReceiveOtherDataToybox(
            new OnlineUserCharaToyboxDataDto(new UserData(UserUID), dto.PatternInfo, dto.UpdateKind)).ConfigureAwait(false);

        _metrics.IncCounter(MetricsAPI.CounterUserPushDataToybox);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataToyboxTo, recipientUids.Count);
    }

    public async Task UserPushPiShockUpdate(UserCharaPiShockPermMessageDto dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // ensure the update type is valid.
        if (dto.UpdateKind != DataUpdateKind.PiShockGlobalUpdated && dto.UpdateKind != DataUpdateKind.PiShockOwnPermsForPairUpdated)
        {
            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Invalid UpdateKind for PiShock Permissions Data!").ConfigureAwait(false);
            return;
        }

        // handle the action differently depending on the update kind.

        // If the UpdateKind is a global update:
        if (dto.UpdateKind == DataUpdateKind.PiShockGlobalUpdated)
        {
            // get the recipient UID list from the recipient list of the dto
            var recipientUidList = dto.Recipients.Select(r => r.UID).ToList();
            bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, recipientUidList, Context.ConnectionAborted).ConfigureAwait(false);
            if (!allCached)
            {
                var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
                recipientUidList = allPairedUsers.Where(f => recipientUidList.Contains(f, StringComparer.Ordinal)).ToList();
                await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
            }

            // REVIEW: We can input checks against active state data here if we run into concurrency issues.
            if (dto.UpdateKind == DataUpdateKind.Safeword)
            {
                var userActiveState = await DbContext.UserActiveStateData.FirstOrDefaultAsync(u => u.UserUID == UserUID).ConfigureAwait(false);
                if (userActiveState == null)
                {
                    await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Cannot update own userActiveStateData!").ConfigureAwait(false);
                    return;
                }
                userActiveState.ToyboxActivePatternId = Guid.Empty;
                // update the database with the new appearance data.
                DbContext.UserActiveStateData.Update(userActiveState);
            }
            _logger.LogCallInfo(GagspeakHubLogger.Args(recipientUidList.Count));
            // we should update all our pairs.
            await Clients.Users(recipientUidList).Client_UserReceiveDataPiShock(new(new UserData(UserUID), dto.ShockPermissions, dto.UpdateKind)).ConfigureAwait(false);
            _metrics.IncCounter(MetricsAPI.CounterUserPushDataPiShock);
            _metrics.IncCounter(MetricsAPI.CounterUserPushDataPiShockTo, recipientUidList.Count);
        }
        else
        {
            // it is a push to update a spesific pair. So we should ensure the Recipients list only contains 1 element.
            if (dto.Recipients.Count != 1)
            {
                await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Should not be notifying multiple users when updating permissions for one!").ConfigureAwait(false);
                return;
            }

            // only search for that user and cache in online synced service.
            var recipientUid = dto.Recipients[0].UID;
            bool isCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(UserUID, new List<string>() { recipientUid }, Context.ConnectionAborted).ConfigureAwait(false);
            if (!isCached)
            {
                var allPairedUsers = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
                await _onlineSyncedPairCacheService.CachePlayers(UserUID, allPairedUsers, Context.ConnectionAborted).ConfigureAwait(false);
            }

            // return the client call to that user.
            await Clients.User(recipientUid).Client_UserReceiveDataPiShock(new
                (new UserData(UserUID), dto.ShockPermissions, DataUpdateKind.PiShockPairPermsForUserUpdated)).ConfigureAwait(false);

            _metrics.IncCounter(MetricsAPI.CounterUserPushDataPiShock);
            _metrics.IncCounter(MetricsAPI.CounterUserPushDataPiShockTo, 1);
        }
    }




    /// <summary>
    /// <b> PLEASE READ THE PROCESS BELOW AS HANDLING THINGS THIS WAY ON THE SERVER IS VERY CRITICAL </b>
    /// <para> Process of updating another pair's permissions. </para>
    /// <list type="number">
    /// <item> Identify the kind of update being made. </item>
    /// <item> Verify the client caller has edit access to make the modification. if not throw an exception. </item>
    /// <item> Update the database row for that pair with the new information. </item>
    /// <item> Save the changes to the database. </item>
    /// <item> Because we don't know the list of the affected pairs online clients, we should check for them via Fetch the list of cached online pairs of </item>
    /// <item> Once we have the list of the pairs online userPairs, push update to all users with the newly updated dto. </item>
    /// </list>
    /// </summary>
    public async Task UserPushPairDataIpcUpdate(OnlineUserCharaIpcDataDto dto)
    {
        // display the ags being passed in.
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Throw exception if attempting to modifier client caller. That's not this functions purpose.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal)) throw new Exception("Cannot update self, only use this to update another pair!");

        // Verify the pairing between these users exists. (Grab permissions via this)
        var pairPermissions = await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPermissions == null) throw new Exception("Cannot update other Pair, No PairPerms exist for you two. Are you paired two-way?");


        // Because the person changing the pairs permission doesnt know all of the pair's added UserPairs, fetch them.
        var allPairsOfAffectedPair = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        // grab the subset of them that are online.
        var allOnlinePairsOfAffectedPair = await GetOnlineUsers(allPairsOfAffectedPair).ConfigureAwait(false);
        var allOnlinePairsOfAffectedPairUids = allOnlinePairsOfAffectedPair.Select(p => p.Key).ToList();

        // Verify all online pairs of pair are cached. If not, cache them.
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            await _onlineSyncedPairCacheService.CachePlayers(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);
        }

        // remove the dto.User from the list of all online pairs, so we can send them a self update message.
        allOnlinePairsOfAffectedPairUids.Remove(dto.User.UID);
        // also grab our client callers userData from the pairPermissions table, preventing a need for a 3rd DB call.
        var clientCallerUserData = pairPermissions.OtherUser.ToUserData();

        // send them a self update message.
        await Clients.User(dto.User.UID).Client_UserReceiveOwnDataIpc(
            new OnlineUserCharaIpcDataDto(clientCallerUserData, dto.IPCData, dto.UpdateKind)).ConfigureAwait(false);

        // Push the updated IPC data out to all the online pairs of the affected pair.
        await Clients.Users(allOnlinePairsOfAffectedPairUids).Client_UserReceiveOtherDataIpc(
            new OnlineUserCharaIpcDataDto(dto.User, dto.IPCData, dto.UpdateKind)).ConfigureAwait(false);

        // Inc the metrics
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataIpc);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataIpcTo, allOnlinePairsOfAffectedPairUids.Count);
    }

    public async Task UserPushPairDataAppearanceUpdate(OnlineUserCharaAppearanceDataDto dto)
    {
        // display the ags being passed in.
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Throw exception if attempting to modifier client caller. That's not this functions purpose.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal)) throw new Exception("Cannot update self, only use this to update another pair!");

        // Verify the pairing between these users exists. (Grab permissions via this)
        var pairPermissions = await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPermissions == null) throw new Exception("Cannot update other Pair, No PairPerms exist for you two. Are you paired two-way?");

        // Fetch affected pair's current appearance data from the DB
        var currentAppearanceData = await DbContext.UserAppearanceData.FirstOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false);
        if (currentAppearanceData == null) throw new Exception("Cannot update other Pair, User does not have appearance data!");

        // Grabs all Pairs of the affected pair
        var allPairsOfAffectedPair = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        // Filter the list to only include online pairs
        var allOnlinePairsOfAffectedPair = await GetOnlineUsers(allPairsOfAffectedPair).ConfigureAwait(false);
        // Convert from Dictionary<string,string> to List<string> of UID's.
        var allOnlinePairsOfAffectedPairUids = allOnlinePairsOfAffectedPair.Select(p => p.Key).ToList();

        // Verify all these pairs are cached for that pair. If not, cache them.
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            await _onlineSyncedPairCacheService.CachePlayers(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);
        }

        // remove the dto.User from the list of all online pairs, so we can send them a self update message.
        allOnlinePairsOfAffectedPairUids.Remove(dto.User.UID);

        // Perform security check on the UpdateKind, to make sure client caller is not exploiting the system.
        switch (dto.UpdateKind)
        {
            // Throw if gag features not allowed OR slot is occupied. Otherwise, update the respective appearance data.
            case DataUpdateKind.AppearanceGagAppliedLayerOne:
                {
                    if (!pairPermissions.GagFeatures)
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Gag Features not modifiable for Pair!").ConfigureAwait(false);
                        return;
                    }
                    if (!string.Equals(currentAppearanceData.SlotOneGagType, None, StringComparison.Ordinal) && !string.Equals(currentAppearanceData.SlotOneGagPadlock, None, StringComparison.Ordinal))
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Slot One is already Locked & cant be replaced occupied!").ConfigureAwait(false);
                        return;
                    }
                    currentAppearanceData.SlotOneGagType = dto.AppearanceData.GagSlots[0].GagType;
                }
                break;
            case DataUpdateKind.AppearanceGagAppliedLayerTwo:
                {
                    if (!pairPermissions.GagFeatures)
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Gag Features not modifiable for Pair!").ConfigureAwait(false);
                        return;
                    }
                    if (!string.Equals(currentAppearanceData.SlotTwoGagType, None, StringComparison.Ordinal) && !string.Equals(currentAppearanceData.SlotTwoGagPadlock, None, StringComparison.Ordinal))
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Slot Two is already occupied!").ConfigureAwait(false);
                        return;
                    }
                    // update the respective appearance data.
                    currentAppearanceData.SlotTwoGagType = dto.AppearanceData.GagSlots[1].GagType;
                }
                break;
            case DataUpdateKind.AppearanceGagAppliedLayerThree:
                {
                    if (!pairPermissions.GagFeatures)
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Gag Features not modifiable for Pair!").ConfigureAwait(false);
                        return;
                    }
                    if (!string.Equals(currentAppearanceData.SlotThreeGagType, None, StringComparison.Ordinal) && !string.Equals(currentAppearanceData.SlotThreeGagPadlock, None, StringComparison.Ordinal))
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Slot Three is already occupied!").ConfigureAwait(false);
                        return;
                    }
                    // update the respective appearance data.
                    currentAppearanceData.SlotThreeGagType = dto.AppearanceData.GagSlots[2].GagType;
                }
                break;
            // Handle lock logic. Throw if lock already present, or if padlock is OwnerPadlock type and OwnerLocks are not allowed.
            case DataUpdateKind.AppearanceGagLockedLayerOne:
                {
                    if (!pairPermissions.GagFeatures) throw new Exception("Gag Features not modifiable for Pair!");
                    if (string.Equals(currentAppearanceData.SlotOneGagType, None, StringComparison.Ordinal)) throw new Exception("No Gag Equipped!");
                    if (!string.Equals(currentAppearanceData.SlotOneGagPadlock, None, StringComparison.Ordinal)) throw new Exception("Slot One is already locked!");

                    // prevent people without OwnerPadlock permission from applying ownerPadlocks.
                    if ((string.Equals(dto.AppearanceData.GagSlots[0].Padlock, OwnerPadlock, StringComparison.Ordinal) && !pairPermissions.OwnerLocks) ||
                        (string.Equals(dto.AppearanceData.GagSlots[0].Padlock, OwnerTimerPadlock, StringComparison.Ordinal) && !pairPermissions.OwnerLocks))
                        throw new Exception("Owner Locks not allowed!");

                    // update the respective appearance data.
                    currentAppearanceData.SlotOneGagPadlock = dto.AppearanceData.GagSlots[0].Padlock;
                    currentAppearanceData.SlotOneGagPassword = dto.AppearanceData.GagSlots[0].Password;
                    currentAppearanceData.SlotOneGagTimer = dto.AppearanceData.GagSlots[0].Timer;
                    currentAppearanceData.SlotOneGagAssigner = dto.AppearanceData.GagSlots[0].Assigner;
                }
                break;
            case DataUpdateKind.AppearanceGagLockedLayerTwo:
                {
                    if (!pairPermissions.GagFeatures) throw new Exception("Gag Features not modifiable for Pair!");
                    if (string.Equals(currentAppearanceData.SlotTwoGagType, None, StringComparison.Ordinal)) throw new Exception("No Gag Equipped!");
                    if (!string.Equals(currentAppearanceData.SlotTwoGagPadlock, None, StringComparison.Ordinal)) throw new Exception("Slot Two is already locked!");

                    // prevent people without OwnerPadlock permission from applying ownerPadlocks.
                    if ((string.Equals(dto.AppearanceData.GagSlots[1].Padlock, OwnerPadlock, StringComparison.Ordinal) && !pairPermissions.OwnerLocks) ||
                        (string.Equals(dto.AppearanceData.GagSlots[1].Padlock, OwnerTimerPadlock, StringComparison.Ordinal) && !pairPermissions.OwnerLocks))
                        throw new Exception("Owner Locks not allowed!");

                    // update the respective appearance data.
                    currentAppearanceData.SlotTwoGagPadlock = dto.AppearanceData.GagSlots[1].Padlock;
                    currentAppearanceData.SlotTwoGagPassword = dto.AppearanceData.GagSlots[1].Password;
                    currentAppearanceData.SlotTwoGagTimer = dto.AppearanceData.GagSlots[1].Timer;
                    currentAppearanceData.SlotTwoGagAssigner = dto.AppearanceData.GagSlots[1].Assigner;
                }
                break;
            case DataUpdateKind.AppearanceGagLockedLayerThree:
                {
                    if (!pairPermissions.GagFeatures) throw new Exception("Gag Features not modifiable for Pair!");
                    if (string.Equals(currentAppearanceData.SlotThreeGagType, None, StringComparison.Ordinal)) throw new Exception("No Gag Equipped!");
                    if (!string.Equals(currentAppearanceData.SlotThreeGagPadlock, None, StringComparison.Ordinal)) throw new Exception("Slot Three is already locked!");
                 
                    // prevent people without OwnerPadlock permission from applying ownerPadlocks.
                    if ((string.Equals(dto.AppearanceData.GagSlots[2].Padlock, OwnerPadlock, StringComparison.Ordinal) && !pairPermissions.OwnerLocks) ||
                        (string.Equals(dto.AppearanceData.GagSlots[2].Padlock, OwnerTimerPadlock, StringComparison.Ordinal) && !pairPermissions.OwnerLocks))
                        throw new Exception("Owner Locks not allowed!");
                    
                    // update the respective appearance data.
                    currentAppearanceData.SlotThreeGagPadlock = dto.AppearanceData.GagSlots[2].Padlock;
                    currentAppearanceData.SlotThreeGagPassword = dto.AppearanceData.GagSlots[2].Password;
                    currentAppearanceData.SlotThreeGagTimer = dto.AppearanceData.GagSlots[2].Timer;
                    currentAppearanceData.SlotThreeGagAssigner = dto.AppearanceData.GagSlots[2].Assigner;
                }
                break;
            // for unlocking, throw if GagFeatures not allowed, the slot is not already locked, or if unlock validation is not met.
            case DataUpdateKind.AppearanceGagUnlockedLayerOne:
                {
                    if (!pairPermissions.GagFeatures)
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Pair doesn't allow you to use GagFeatures on them!").ConfigureAwait(false);
                        return;
                    }
                    if (string.Equals(currentAppearanceData.SlotOneGagType, None, StringComparison.Ordinal))
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "No Gag Equipped!").ConfigureAwait(false);
                        return;
                    }
                    if (!string.Equals(currentAppearanceData.SlotOneGagPadlock, dto.AppearanceData.GagSlots[0].Password, StringComparison.Ordinal))
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Password incorrect!").ConfigureAwait(false);
                        return;
                    }
                    if ((string.Equals(currentAppearanceData.SlotOneGagPadlock, OwnerPadlock, StringComparison.Ordinal)
                      || string.Equals(currentAppearanceData.SlotOneGagPadlock, OwnerTimerPadlock, StringComparison.Ordinal)))
                    {
                        // prevent unlock if OwnerLocks are not allowed.
                        if (!pairPermissions.OwnerLocks)
                        {
                            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "You cannot unlock OwnerPadlock types, pair doesn't allow you!").ConfigureAwait(false);
                            return;
                        }
                        // otherwise, throw exception if the client caller userUID does not match the assigner.
                        if (!string.Equals(currentAppearanceData.SlotOneGagAssigner, UserUID, StringComparison.Ordinal))
                        {
                            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "You are not the assigner of this OwnerPadlock!").ConfigureAwait(false);
                            return;
                        }
                    }
                    // Update the respective appearance data.
                    currentAppearanceData.SlotOneGagPadlock = None;
                    currentAppearanceData.SlotOneGagPassword = string.Empty;
                    currentAppearanceData.SlotOneGagTimer = DateTimeOffset.UtcNow;
                    currentAppearanceData.SlotOneGagAssigner = string.Empty;
                }
                break;
            case DataUpdateKind.AppearanceGagUnlockedLayerTwo:
                {
                    if (!pairPermissions.GagFeatures)
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Pair doesn't allow you to use GagFeatures on them!").ConfigureAwait(false);
                        return;
                    }
                    if (string.Equals(currentAppearanceData.SlotTwoGagType, None, StringComparison.Ordinal))
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "No Gag Equipped!").ConfigureAwait(false);
                        return;
                    }
                    if (!string.Equals(currentAppearanceData.SlotTwoGagPassword, dto.AppearanceData.GagSlots[1].Password, StringComparison.Ordinal))
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Password incorrect!").ConfigureAwait(false);
                        return;
                    }
                    if ((string.Equals(currentAppearanceData.SlotTwoGagPadlock, OwnerPadlock, StringComparison.Ordinal)
                      || string.Equals(currentAppearanceData.SlotTwoGagPadlock, OwnerTimerPadlock, StringComparison.Ordinal)))
                    {
                        // prevent unlock if OwnerLocks are not allowed.
                        if (!pairPermissions.OwnerLocks)
                        {
                            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "You cannot unlock OwnerPadlock types, pair doesn't allow you!").ConfigureAwait(false);
                            return;
                        }
                        // otherwise, throw exception if the client caller userUID does not match the assigner.
                        if (!string.Equals(currentAppearanceData.SlotTwoGagAssigner, UserUID, StringComparison.Ordinal))
                        {
                            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "You are not the assigner of this OwnerPadlock!").ConfigureAwait(false);
                            return;
                        }
                    }
                    // Update the respective appearance data.
                    currentAppearanceData.SlotTwoGagPadlock = None;
                    currentAppearanceData.SlotTwoGagPassword = string.Empty;
                    currentAppearanceData.SlotTwoGagTimer = DateTimeOffset.UtcNow;
                    currentAppearanceData.SlotTwoGagAssigner = string.Empty;
                }
                break;
            case DataUpdateKind.AppearanceGagUnlockedLayerThree:
                {
                    if (!pairPermissions.GagFeatures)
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Pair doesn't allow you to use GagFeatures on them!").ConfigureAwait(false);
                        return;
                    }
                    if (string.Equals(currentAppearanceData.SlotThreeGagType, None, StringComparison.Ordinal))
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "No Gag Equipped!").ConfigureAwait(false); 
                        return;
                    }
                    if (!string.Equals(currentAppearanceData.SlotThreeGagPassword, dto.AppearanceData.GagSlots[2].Password, StringComparison.Ordinal))
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Password incorrect!").ConfigureAwait(false);
                        return;
                    }
                    if ((string.Equals(currentAppearanceData.SlotThreeGagPadlock, OwnerPadlock, StringComparison.Ordinal)
                      || string.Equals(currentAppearanceData.SlotThreeGagPadlock, OwnerTimerPadlock, StringComparison.Ordinal)))
                    {
                        // prevent unlock if OwnerLocks are not allowed.
                        if (!pairPermissions.OwnerLocks)
                        {
                            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "You cannot unlock OwnerPadlock types, pair doesn't allow you!").ConfigureAwait(false);
                            return;
                        }
                        // otherwise, throw exception if the client caller userUID does not match the assigner.
                        if (!string.Equals(currentAppearanceData.SlotThreeGagAssigner, UserUID, StringComparison.Ordinal))
                        {
                            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "You are not the assigner of this OwnerPadlock!").ConfigureAwait(false);
                            return;
                        }
                    }

                    // Update the respective appearance data.
                    currentAppearanceData.SlotThreeGagPadlock = None;
                    currentAppearanceData.SlotThreeGagPassword = string.Empty;
                    currentAppearanceData.SlotThreeGagTimer = DateTimeOffset.UtcNow;
                    currentAppearanceData.SlotThreeGagAssigner = string.Empty;
                }
                break;
            // for removal, throw if gag is locked, or if GagFeatures not allowed.
            case DataUpdateKind.AppearanceGagRemovedLayerOne:
                {
                    // Throw if GagFeatures not allowed
                    if (!pairPermissions.GagFeatures) throw new Exception("Pair doesn't allow you to use GagFeatures on them!");
                    // Throw if slot is locked
                    if (!string.Equals(currentAppearanceData.SlotOneGagPadlock, None, StringComparison.Ordinal)) throw new Exception("Slot One is locked!");
                    // Update the respective appearance data.
                    currentAppearanceData.SlotOneGagType = None;
                    currentAppearanceData.SlotOneGagAssigner = string.Empty;
                }
                break;
            case DataUpdateKind.AppearanceGagRemovedLayerTwo:
                {
                    // Throw if GagFeatures not allowed
                    if (!pairPermissions.GagFeatures) throw new Exception("Pair doesn't allow you to use GagFeatures on them!");
                    // Throw if slot is locked
                    if (!string.Equals(currentAppearanceData.SlotTwoGagPadlock, None, StringComparison.Ordinal)) throw new Exception("Slot Two is locked!");
                    // Update the respective appearance data.
                    currentAppearanceData.SlotTwoGagType = None;
                    currentAppearanceData.SlotTwoGagAssigner = string.Empty;
                }
                break;
            case DataUpdateKind.AppearanceGagRemovedLayerThree:
                {
                    // Throw if GagFeatures not allowed
                    if (!pairPermissions.GagFeatures) throw new Exception("Pair doesn't allow you to use GagFeatures on them!");
                    // Throw if slot is locked
                    if (!string.Equals(currentAppearanceData.SlotThreeGagPadlock, None, StringComparison.Ordinal)) throw new Exception("Slot Three is locked!");
                    // Update the respective appearance data.
                    currentAppearanceData.SlotThreeGagType = None;
                    currentAppearanceData.SlotThreeGagAssigner = string.Empty;
                }
                break;
            default:
                throw new Exception("Invalid UpdateKind for Appearance Data!");
        }

        // update the database with the new appearance data.
        DbContext.UserAppearanceData.Update(currentAppearanceData);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // migrate the current appearance data with its changes to a new dto object for sending
        var updatedAppearanceData = new CharacterAppearanceData()
        {
            GagSlots = new GagSlot[]
            {
                new GagSlot
                {
                    GagType = currentAppearanceData.SlotOneGagType,
                    Padlock = currentAppearanceData.SlotOneGagPadlock,
                    Password = currentAppearanceData.SlotOneGagPassword,
                    Timer = currentAppearanceData.SlotOneGagTimer,
                    Assigner = currentAppearanceData.SlotOneGagAssigner
                },
                new GagSlot
                {
                    GagType = currentAppearanceData.SlotTwoGagType,
                    Padlock = currentAppearanceData.SlotTwoGagPadlock,
                    Password = currentAppearanceData.SlotTwoGagPassword,
                    Timer = currentAppearanceData.SlotTwoGagTimer,
                    Assigner = currentAppearanceData.SlotTwoGagAssigner
                },
                new GagSlot
                {
                    GagType = currentAppearanceData.SlotThreeGagType,
                    Padlock = currentAppearanceData.SlotThreeGagPadlock,
                    Password = currentAppearanceData.SlotThreeGagPassword,
                    Timer = currentAppearanceData.SlotThreeGagTimer,
                    Assigner = currentAppearanceData.SlotThreeGagAssigner
                }
            }
        };

        // send them a self update message.
        await Clients.User(dto.User.UID).Client_UserReceiveOwnDataAppearance(
            new OnlineUserCharaAppearanceDataDto(UserUID.ToUserDataFromUID(), updatedAppearanceData, dto.UpdateKind)).ConfigureAwait(false);

        // Push the updated IPC data out to all the online pairs of the affected pair.
        await Clients.Users(allOnlinePairsOfAffectedPairUids).Client_UserReceiveOtherDataAppearance(
            new OnlineUserCharaAppearanceDataDto(dto.User, updatedAppearanceData, dto.UpdateKind)).ConfigureAwait(false);

        // Inc the metrics
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataAppearance);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataAppearanceTo, allOnlinePairsOfAffectedPairUids.Count);
    }

    public async Task UserPushPairDataWardrobeUpdate(OnlineUserCharaWardrobeDataDto dto)
    {
        // display the ags being passed in.
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Throw exception if attempting to modifier client caller. That's not this functions purpose.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal)) throw new Exception("Cannot update self, only use this to update another pair!");

        // Verify the pairing between these users exists. (Grab permissions via this)
        var pairPermissions = await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPermissions == null) throw new Exception("Cannot update other Pair, No PairPerms exist for you two. Are you paired two-way?");

        // Fetch affected pair's current activeState data from the DB
        var userActiveState = await DbContext.UserActiveStateData.FirstOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false);
        if (userActiveState == null) throw new Exception("User has no Active State Data!");

        // Grabs all Pairs of the affected pair
        var allPairsOfAffectedPair = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        // Filter the list to only include online pairs
        var allOnlinePairsOfAffectedPair = await GetOnlineUsers(allPairsOfAffectedPair).ConfigureAwait(false);
        // Convert from Dictionary<string,string> to List<string> of UID's.
        var allOnlinePairsOfAffectedPairUids = allOnlinePairsOfAffectedPair.Select(p => p.Key).ToList();

        // Verify all these pairs are cached for that pair. If not, cache them.
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            await _onlineSyncedPairCacheService.CachePlayers(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);
        }

        // remove the dto.User from the list of all online pairs, so we can send them a self update message.
        allOnlinePairsOfAffectedPairUids.Remove(dto.User.UID);

        // Perform security check on the UpdateKind, to make sure client caller is not exploiting the system.
        switch (dto.UpdateKind)
        {
            // Throw if WardrobeApplying is not allowed for pair.
            case DataUpdateKind.WardrobeRestraintApplied:
                {
                    // Throw if permission to apply sets is not granted.
                    if (!pairPermissions.ApplyRestraintSets)
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Pair doesn't allow you to use WardrobeApplying on them!").ConfigureAwait(false);
                        return;
                    }
                    // throw if the userActiveStateData's activeSet exists and is locked.
                    if (!string.IsNullOrEmpty(userActiveState.WardrobeActiveSetName) && !string.Equals(userActiveState.WardrobeActiveSetPadLock, Padlocks.None.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Cannot Replace Currently Active Set because it is currently locked!").ConfigureAwait(false);
                        return;
                    }
                    // Perform the update logic for the wardrobe data.
                    userActiveState.WardrobeActiveSetName = dto.WardrobeData.ActiveSetName;
                    userActiveState.WardrobeActiveSetAssigner = dto.WardrobeData.ActiveSetEnabledBy;
                }
                break;
            case DataUpdateKind.WardrobeRestraintLocked:
                {
                    if (!pairPermissions.LockRestraintSets)
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Pair doesn't allow you to use WardrobeLocking on them!").ConfigureAwait(false);
                        return;
                    }
                    if (userActiveState.WardrobeActiveSetName.IsNullOrEmpty())
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "No active set to lock!").ConfigureAwait(false); 
                        return;
                    }
                    if (!string.Equals(userActiveState.WardrobeActiveSetPadLock, None, StringComparison.Ordinal))
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Active set is already locked!").ConfigureAwait(false);
                        return;
                    }
                    // prevent people without OwnerPadlock permission from applying ownerPadlocks.
                    if ((string.Equals(dto.WardrobeData.Padlock, OwnerPadlock, StringComparison.Ordinal) && !pairPermissions.OwnerLocks) ||
                        (string.Equals(dto.WardrobeData.Padlock, OwnerTimerPadlock, StringComparison.Ordinal) && !pairPermissions.OwnerLocks))
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "You cannot lock OwnerPadlock types, pair doesn't allow you!").ConfigureAwait(false);
                        return;
                    }
                    // update the respective appearance data.
                    userActiveState.WardrobeActiveSetPadLock = dto.WardrobeData.Padlock;
                    userActiveState.WardrobeActiveSetPassword = dto.WardrobeData.Password;
                    userActiveState.WardrobeActiveSetLockTime = dto.WardrobeData.Timer;
                    userActiveState.WardrobeActiveSetLockAssigner = dto.WardrobeData.Assigner;
                }
                break;
            case DataUpdateKind.WardrobeRestraintUnlocked:
                {
                    // Throw if GagFeatures not allowed
                    if (!pairPermissions.UnlockRestraintSets)
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Pair doesn't allow you to use WardrobeUnlocking on them!").ConfigureAwait(false);
                        return;
                    }
                    // Throw if slot is not already locked
                    if (string.Equals(userActiveState.WardrobeActiveSetPadLock, None, StringComparison.Ordinal))
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Active set is already unlocked!").ConfigureAwait(false);
                        return;
                    }
                    // Throw if password doesn't match existing password.
                    if (!string.Equals(userActiveState.WardrobeActiveSetPassword, dto.WardrobeData.Password, StringComparison.Ordinal))
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Password incorrect!").ConfigureAwait(false);
                        return;
                    }
                    // Throw if type is ownerPadlock or OwnerTimerPadlock, and OwnerLocks are not allowed.
                    if ((string.Equals(userActiveState.WardrobeActiveSetPadLock, OwnerPadlock, StringComparison.Ordinal)
                      || string.Equals(userActiveState.WardrobeActiveSetPadLock, OwnerTimerPadlock, StringComparison.Ordinal)))
                    {
                        // prevent unlock if OwnerLocks are not allowed.
                        if (!pairPermissions.OwnerLocks)
                        {
                            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "You cannot unlock OwnerPadlock types, pair doesn't allow you!").ConfigureAwait(false);
                            return;
                        }
                        // otherwise, throw exception if the client caller userUID does not match the assigner.
                        if (!string.Equals(userActiveState.WardrobeActiveSetAssigner, UserUID, StringComparison.Ordinal))
                        {
                            await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "You are not the assigner of this OwnerPadlock!").ConfigureAwait(false);
                            return;
                        }
                    }

                    // update the respective active wardrobe data.
                    userActiveState.WardrobeActiveSetPadLock = None;
                    userActiveState.WardrobeActiveSetPassword = string.Empty;
                    userActiveState.WardrobeActiveSetLockTime = DateTimeOffset.MinValue;
                    userActiveState.WardrobeActiveSetAssigner = string.Empty;
                }
                break;
            case DataUpdateKind.WardrobeRestraintDisabled:
                {
                    // Throw if permission to remove sets is not granted.
                    if (!pairPermissions.RemoveRestraintSets)
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Pair doesn't allow you to use WardrobeRemoving on them!").ConfigureAwait(false);
                        return;
                    }

                    // Throw if no set is active
                    if (string.IsNullOrEmpty(userActiveState.WardrobeActiveSetName))
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "No active set to remove!").ConfigureAwait(false);
                        return;
                    }
                    // Throw if set is still locked
                    if (!string.Equals(userActiveState.WardrobeActiveSetPadLock, None, StringComparison.Ordinal))
                    {
                        await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Warning, "Active set is still locked!").ConfigureAwait(false);
                        return;
                    }
                    // Perform the update logic for the wardrobe data.
                    userActiveState.WardrobeActiveSetName = string.Empty;
                    userActiveState.WardrobeActiveSetAssigner = string.Empty;
                }
                break;
            default:
                throw new Exception("Invalid UpdateKind for Wardrobe Data!");
        }

        // build new DTO to send off.
        var updatedWardrobeData = new CharacterWardrobeData()
        {
            OutfitNames = dto.WardrobeData.OutfitNames, // this becomes irrelevant since none of these settings change this.
            ActiveSetName = userActiveState.WardrobeActiveSetName,
            ActiveSetEnabledBy = userActiveState.WardrobeActiveSetAssigner,
            Padlock = userActiveState.WardrobeActiveSetPadLock,
            Password = userActiveState.WardrobeActiveSetPassword,
            Timer = userActiveState.WardrobeActiveSetLockTime,
            Assigner = userActiveState.WardrobeActiveSetLockAssigner,
        };

        // update the changes to the database.
        DbContext.UserActiveStateData.Update(userActiveState);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // send them a self update message, setting us as the user who appled, with the updated wardrobe data.
        await Clients.User(dto.User.UID).Client_UserReceiveOwnDataWardrobe(
            new OnlineUserCharaWardrobeDataDto(UserUID.ToUserDataFromUID(), updatedWardrobeData, dto.UpdateKind)).ConfigureAwait(false);

        // Push the updated wardrobe data out to all the online pairs of the affected pair.
        await Clients.Users(allOnlinePairsOfAffectedPairUids).Client_UserReceiveOtherDataWardrobe(
            new OnlineUserCharaWardrobeDataDto(dto.User, updatedWardrobeData, dto.UpdateKind)).ConfigureAwait(false);

        // Inc the metrics
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataWardrobe);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataWardrobeTo, allOnlinePairsOfAffectedPairUids.Count);
    }

    public async Task UserPushPairDataAliasStorageUpdate(OnlineUserCharaAliasDataDto dto)
    {
        // display the ags being passed in.
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Throw exception if attempting to modifier client caller. That's not this functions purpose.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal)) throw new Exception("Cannot update self, only use this to update another pair!");

        // verify that a pair between the two clients is made.
        var pairPermissions = await DbContext.ClientPairs.FirstOrDefaultAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPermissions == null) throw new Exception("Cannot update other Pair, No PairPerms exist for you two. Are you paired two-way?");

        // ensure that the update kind of to change the registered names. If it is not, throw exception.
        if (dto.UpdateKind != DataUpdateKind.PuppeteerPlayerNameRegistered) throw new Exception("Invalid UpdateKind for Alias Data!");

        // in our dto, we have the PAIR WE ARE PROVIDING OUR NAME TO as the userdata, with our name info inside.
        // so when we construct the message to update the client's OWN data, we need to place the client callers name info inside.
        await Clients.User(dto.User.UID).Client_UserReceiveOwnDataAlias(
            new OnlineUserCharaAliasDataDto(new UserData(UserUID), dto.AliasData, dto.UpdateKind)).ConfigureAwait(false);

        // when we push the update back to our client caller, we must inform them that the client callers name was updated.
        await Clients.Caller.Client_UserReceiveOtherDataAlias(new OnlineUserCharaAliasDataDto(dto.User, dto.AliasData, dto.UpdateKind)).ConfigureAwait(false);
    }

    public async Task UserPushPairDataToyboxUpdate(OnlineUserCharaToyboxDataDto dto)
    {
        // display the ags being passed in.
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // Throw exception if attempting to modifier client caller. That's not this functions purpose.
        if (string.Equals(dto.User.UID, UserUID, StringComparison.Ordinal)) throw new Exception("Cannot update self, only use this to update another pair!");

        // Verify the pairing between these users exists. (Grab permissions via this)
        var pairPermissions = await DbContext.ClientPairPermissions.FirstOrDefaultAsync(p => p.UserUID == dto.User.UID && p.OtherUserUID == UserUID).ConfigureAwait(false);
        if (pairPermissions == null) throw new Exception("Cannot update other Pair, No PairPerms exist for you two. Are you paired two-way?");

        // Fetch affected pair's current activeState data from the DB
        var userActiveState = await DbContext.UserActiveStateData.FirstOrDefaultAsync(u => u.UserUID == dto.User.UID).ConfigureAwait(false);
        if (userActiveState == null) throw new Exception("User has no Active State Data!");

        // Grabs all Pairs of the affected pair
        var allPairsOfAffectedPair = await GetAllPairedUnpausedUsers(dto.User.UID).ConfigureAwait(false);
        // Filter the list to only include online pairs
        var allOnlinePairsOfAffectedPair = await GetOnlineUsers(allPairsOfAffectedPair).ConfigureAwait(false);
        // Convert from Dictionary<string,string> to List<string> of UID's.
        var allOnlinePairsOfAffectedPairUids = allOnlinePairsOfAffectedPair.Select(p => p.Key).ToList();

        // Verify all these pairs are cached for that pair. If not, cache them.
        bool allCached = await _onlineSyncedPairCacheService.AreAllPlayersCached(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);
        if (!allCached)
        {
            await _onlineSyncedPairCacheService.CachePlayers(dto.User.UID, allOnlinePairsOfAffectedPairUids, Context.ConnectionAborted).ConfigureAwait(false);
        }

        // remove the dto.User from the list of all online pairs, so we can send them a self update message.
        allOnlinePairsOfAffectedPairUids.Remove(dto.User.UID);

        // Perform security check on the UpdateKind, to make sure client caller is not exploiting the system.
        switch (dto.UpdateKind)
        {
            case DataUpdateKind.ToyboxPatternExecuted:
                {
                    if (!pairPermissions.CanExecutePatterns) throw new Exception("Pair doesn't allow you to use ToyboxPatternFeatures on them!");
                    // ensure that we have a pattern set to active that should be set to active.
                    var activePattern = dto.ToyboxInfo.ActivePatternGuid;
                    if(activePattern == Guid.Empty) throw new Exception("Cannot activate Guid.Empty!");

                    // otherwise, we should activate that pattern in our user state.
                    userActiveState.ToyboxActivePatternId = activePattern;
                }
                break;
            case DataUpdateKind.ToyboxPatternStopped:
                {
                    if (!pairPermissions.CanExecutePatterns) throw new Exception("Pair doesn't allow you to use ToyboxPatternFeatures on them!");
                    // Throw if no pattern was playing.
                    if (userActiveState.ToyboxActivePatternId == Guid.Empty) throw new Exception("No active pattern was playing, nothing to stop!");
                    // If we reach here, the passed in data is valid (all patterns should no longer be active) and we can send back the data.
                    userActiveState.ToyboxActivePatternId = Guid.Empty;
                }
                break;
            case DataUpdateKind.ToyboxAlarmListUpdated: throw new Exception("Cannot modify this type of data here!");
            case DataUpdateKind.ToyboxAlarmToggled:
                if (!pairPermissions.CanToggleAlarms) throw new Exception("Pair doesn't allow you to use ToyboxAlarmFeatures on them!");
                break;
            case DataUpdateKind.ToyboxTriggerListUpdated: throw new Exception("Cannot modify this type of data here!");
            case DataUpdateKind.ToyboxTriggerToggled:
                if (!pairPermissions.CanToggleTriggers) throw new Exception("Pair doesn't allow you to use ToyboxTriggerFeatures on them!");
                break;
            default:
                throw new Exception("Invalid UpdateKind for Toybox Data!");
        }

        // update the changes to the database.
        DbContext.UserActiveStateData.Update(userActiveState);
        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        // Message SHOULDNT change, so should just need to send back Dto.

        // send them a self update message.
        await Clients.User(dto.User.UID).Client_UserReceiveOwnDataToybox(
            new OnlineUserCharaToyboxDataDto(UserUID.ToUserDataFromUID(), dto.ToyboxInfo, dto.UpdateKind)).ConfigureAwait(false);

        // Push the updated IPC data out to all the online pairs of the affected pair.
        await Clients.Users(allOnlinePairsOfAffectedPairUids).Client_UserReceiveOtherDataToybox(
            new OnlineUserCharaToyboxDataDto(dto.User, dto.ToyboxInfo, dto.UpdateKind)).ConfigureAwait(false);

        // Inc the metrics
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataToybox);
        _metrics.IncCounter(MetricsAPI.CounterUserPushDataToyboxTo, allOnlinePairsOfAffectedPairUids.Count);
    }
}