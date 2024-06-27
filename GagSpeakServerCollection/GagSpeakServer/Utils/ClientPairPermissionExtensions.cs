using GagspeakShared.Models;

namespace GagspeakServer.Utils;
/// <summary>
/// Extention helper method for converting ClientPairPermissions to UserPermissionsComposite. for DTO's
/// </summary>
#pragma warning disable MA0051; // Method is too long
public static class ClientPairPermissionExtensions
{
    public static GagSpeak.API.Data.Permissions.UserGlobalPermissions ToApiGlobalPerms(this UserGlobalPermissions? globalPermsModel)
    {
        if (globalPermsModel == null) return new GagSpeak.API.Data.Permissions.UserGlobalPermissions();

        GagSpeak.API.Data.Permissions.UserGlobalPermissions result = new GagSpeak.API.Data.Permissions.UserGlobalPermissions();

        result.Safeword = globalPermsModel.Safeword;
        result.SafewordUsed = globalPermsModel.SafewordUsed;
        result.CommandsFromFriends = globalPermsModel.CommandsFromFriends;
        result.CommandsFromParty = globalPermsModel.CommandsFromParty;
        result.LiveChatGarblerActive = globalPermsModel.LiveChatGarblerActive;
        result.LiveChatGarblerLocked = globalPermsModel.LiveChatGarblerLocked;

        result.WardrobeEnabled = globalPermsModel.WardrobeEnabled;
        result.ItemAutoEquip = globalPermsModel.ItemAutoEquip;
        result.RestraintSetAutoEquip = globalPermsModel.RestraintSetAutoEquip;
        result.LockGagStorageOnGagLock = globalPermsModel.LockGagStorageOnGagLock;

        result.PuppeteerEnabled = globalPermsModel.PuppeteerEnabled;
        result.GlobalTriggerPhrase = globalPermsModel.GlobalTriggerPhrase;
        result.GlobalAllowSitRequests = globalPermsModel.GlobalAllowSitRequests;
        result.GlobalAllowMotionRequests = globalPermsModel.GlobalAllowMotionRequests;
        result.GlobalAllowAllRequests = globalPermsModel.GlobalAllowAllRequests;

        result.MoodlesEnabled = globalPermsModel.MoodlesEnabled;

        result.ToyboxEnabled = globalPermsModel.ToyboxEnabled;
        result.LockToyboxUI = globalPermsModel.LockToyboxUI;
        result.ToyIsActive = globalPermsModel.ToyIsActive;
        result.ToyIntensity = globalPermsModel.ToyIntensity;
        result.SpatialVibratorAudio = globalPermsModel.SpatialVibratorAudio;

        return result;
    }


    public static GagSpeak.API.Data.Permissions.UserPairPermissions ToApiUserPairPerms(this ClientPairPermissions? clientPairPermsModel)
    {
        // if null, return a fresh UserPermissionsComposite object.
        if (clientPairPermsModel == null) return new GagSpeak.API.Data.Permissions.UserPairPermissions();

        // create a new UserPermissionsComposite object
        GagSpeak.API.Data.Permissions.UserPairPermissions result = new GagSpeak.API.Data.Permissions.UserPairPermissions();

        // set the variables
        result.ExtendedLockTimes = clientPairPermsModel.ExtendedLockTimes;
        result.MaxLockTime = clientPairPermsModel.MaxLockTime;
        result.InHardcore = clientPairPermsModel.InHardcore;

        result.ApplyRestraintSets = clientPairPermsModel.ApplyRestraintSets;
        result.LockRestraintSets = clientPairPermsModel.LockRestraintSets;
        result.MaxAllowedRestraintTime = clientPairPermsModel.MaxAllowedRestraintTime;
        result.RemoveRestraintSets = clientPairPermsModel.RemoveRestraintSets;

        result.TriggerPhrase = clientPairPermsModel.TriggerPhrase;
        result.StartChar = clientPairPermsModel.StartChar;
        result.EndChar = clientPairPermsModel.EndChar;
        result.AllowSitRequests = clientPairPermsModel.AllowSitRequests;
        result.AllowMotionRequests = clientPairPermsModel.AllowMotionRequests;
        result.AllowAllRequests = clientPairPermsModel.AllowAllRequests;

        result.AllowPositiveStatusTypes = clientPairPermsModel.AllowPositiveStatusTypes;
        result.AllowNegativeStatusTypes = clientPairPermsModel.AllowNegativeStatusTypes;
        result.AllowSpecialStatusTypes = clientPairPermsModel.AllowSpecialStatusTypes;
        result.PairCanApplyOwnMoodlesToYou = clientPairPermsModel.PairCanApplyOwnMoodlesToYou;
        result.PairCanApplyYourMoodlesToYou = clientPairPermsModel.PairCanApplyYourMoodlesToYou;
        result.MaxMoodleTime = clientPairPermsModel.MaxMoodleTime;
        result.AllowPermanentMoodles = clientPairPermsModel.AllowPermanentMoodles;

        result.ChangeToyState = clientPairPermsModel.ChangeToyState;
        result.CanControlIntensity = clientPairPermsModel.CanControlIntensity;
        result.VibratorAlarms = clientPairPermsModel.VibratorAlarms;
        result.CanUseRealtimeVibeRemote = clientPairPermsModel.CanUseRealtimeVibeRemote;
        result.CanExecutePatterns = clientPairPermsModel.CanExecutePatterns;
        result.CanExecuteTriggers = clientPairPermsModel.CanExecuteTriggers;
        result.CanCreateTriggers = clientPairPermsModel.CanCreateTriggers;
        result.CanSendTriggers = clientPairPermsModel.CanSendTriggers;

        result.AllowForcedFollow = clientPairPermsModel.AllowForcedFollow;
        result.IsForcedToFollow = clientPairPermsModel.IsForcedToFollow;
        result.AllowForcedSit = clientPairPermsModel.AllowForcedSit;
        result.IsForcedToSit = clientPairPermsModel.IsForcedToSit;
        result.AllowForcedToStay = clientPairPermsModel.AllowForcedToStay;
        result.IsForcedToStay = clientPairPermsModel.IsForcedToStay;
        result.AllowBlindfold = clientPairPermsModel.AllowBlindfold;
        result.ForceLockFirstPerson = clientPairPermsModel.ForceLockFirstPerson;
        result.IsBlindfolded = clientPairPermsModel.IsBlindfolded;

        return result;
    }

    public static GagSpeak.API.Data.Permissions.UserEditAccessPermissions ToApiUserPairEditAccessPerms(this ClientPairPermissionAccess pairAccessPermsModel)
    {
        if (pairAccessPermsModel == null) return new GagSpeak.API.Data.Permissions.UserEditAccessPermissions();

        // create new UserPermissionsEditAccessComposite object
        GagSpeak.API.Data.Permissions.UserEditAccessPermissions result = new GagSpeak.API.Data.Permissions.UserEditAccessPermissions();

        // set all subaccess perms to defaults
        result.CommandsFromFriendsAllowed = pairAccessPermsModel.CommandsFromFriendsAllowed;
        result.CommandsFromPartyAllowed = pairAccessPermsModel.CommandsFromPartyAllowed;
        result.LiveChatGarblerActiveAllowed = pairAccessPermsModel.LiveChatGarblerActiveAllowed;
        result.LiveChatGarblerLockedAllowed = pairAccessPermsModel.LiveChatGarblerLockedAllowed;
        result.ExtendedLockTimesAllowed = pairAccessPermsModel.ExtendedLockTimesAllowed;
        result.MaxLockTimeAllowed = pairAccessPermsModel.MaxLockTimeAllowed;

        result.WardrobeEnabledAllowed = pairAccessPermsModel.WardrobeEnabledAllowed;
        result.ItemAutoEquipAllowed = pairAccessPermsModel.ItemAutoEquipAllowed;
        result.RestraintSetAutoEquipAllowed = pairAccessPermsModel.RestraintSetAutoEquipAllowed;
        result.LockGagStorageOnGagLockAllowed = pairAccessPermsModel.LockGagStorageOnGagLockAllowed;
        result.ApplyRestraintSetsAllowed = pairAccessPermsModel.ApplyRestraintSetsAllowed;
        result.LockRestraintSetsAllowed = pairAccessPermsModel.LockRestraintSetsAllowed;
        result.MaxAllowedRestraintTimeAllowed = pairAccessPermsModel.MaxAllowedRestraintTimeAllowed;
        result.RemoveRestraintSetsAllowed = pairAccessPermsModel.RemoveRestraintSetsAllowed;

        result.PuppeteerEnabledAllowed = pairAccessPermsModel.PuppeteerEnabledAllowed;
        result.AllowSitRequestsAllowed = pairAccessPermsModel.AllowSitRequestsAllowed;
        result.AllowMotionRequestsAllowed = pairAccessPermsModel.AllowMotionRequestsAllowed;
        result.AllowAllRequestsAllowed = pairAccessPermsModel.AllowAllRequestsAllowed;

        result.MoodlesEnabledAllowed = pairAccessPermsModel.MoodlesEnabledAllowed;
        result.AllowPositiveStatusTypesAllowed = pairAccessPermsModel.AllowPositiveStatusTypesAllowed;
        result.AllowNegativeStatusTypesAllowed = pairAccessPermsModel.AllowNegativeStatusTypesAllowed;
        result.AllowSpecialStatusTypesAllowed = pairAccessPermsModel.AllowSpecialStatusTypesAllowed;
        result.PairCanApplyOwnMoodlesToYouAllowed = pairAccessPermsModel.PairCanApplyOwnMoodlesToYouAllowed;
        result.PairCanApplyYourMoodlesToYouAllowed = pairAccessPermsModel.PairCanApplyYourMoodlesToYouAllowed;
        result.MaxMoodleTimeAllowed = pairAccessPermsModel.MaxMoodleTimeAllowed;
        result.AllowPermanentMoodlesAllowed = pairAccessPermsModel.AllowPermanentMoodlesAllowed;

        result.ToyboxEnabledAllowed = pairAccessPermsModel.ToyboxEnabledAllowed;
        result.LockToyboxUIAllowed = pairAccessPermsModel.LockToyboxUIAllowed;
        result.ToyIsActiveAllowed = pairAccessPermsModel.ToyIsActiveAllowed;
        result.SpatialVibratorAudioAllowed = pairAccessPermsModel.SpatialVibratorAudioAllowed;
        result.ChangeToyStateAllowed = pairAccessPermsModel.ChangeToyStateAllowed;
        result.CanControlIntensityAllowed = pairAccessPermsModel.CanControlIntensityAllowed;
        result.VibratorAlarmsAllowed = pairAccessPermsModel.VibratorAlarmsAllowed;
        result.CanUseRealtimeVibeRemoteAllowed = pairAccessPermsModel.CanUseRealtimeVibeRemoteAllowed;
        result.CanExecutePatternsAllowed = pairAccessPermsModel.CanExecutePatternsAllowed;
        result.CanExecuteTriggersAllowed = pairAccessPermsModel.CanExecuteTriggersAllowed;
        result.CanCreateTriggersAllowed = pairAccessPermsModel.CanCreateTriggersAllowed;
        result.CanSendTriggersAllowed = pairAccessPermsModel.CanSendTriggersAllowed;

        // return it
        return result;
    }

}
#pragma warning restore MA0051; // Method is too long