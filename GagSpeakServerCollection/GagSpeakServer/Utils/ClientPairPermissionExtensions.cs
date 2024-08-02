using GagspeakShared.Models;

namespace GagspeakServer.Utils;
/// <summary>
/// Extention helper method for converting ClientPairPermissions to UserPermissionsComposite. for DTO's
/// </summary>
#pragma warning disable MA0051; // Method is too long
public static class ClientPairPermissionExtensions
{
    #region GlobalPermissionMigrations
    public static GagspeakAPI.Data.Permissions.UserGlobalPermissions ToApiGlobalPerms(this UserGlobalPermissions? globalPermsModel)
    {
        if (globalPermsModel == null) return new GagspeakAPI.Data.Permissions.UserGlobalPermissions();

        GagspeakAPI.Data.Permissions.UserGlobalPermissions result = new GagspeakAPI.Data.Permissions.UserGlobalPermissions();

        result.Safeword = globalPermsModel.Safeword;
        result.SafewordUsed = globalPermsModel.SafewordUsed;
        result.CommandsFromFriends = globalPermsModel.CommandsFromFriends;
        result.CommandsFromParty = globalPermsModel.CommandsFromParty;
        result.LiveChatGarblerActive = globalPermsModel.LiveChatGarblerActive;
        result.LiveChatGarblerLocked = globalPermsModel.LiveChatGarblerLocked;

        result.WardrobeEnabled = globalPermsModel.WardrobeEnabled;
        result.ItemAutoEquip = globalPermsModel.ItemAutoEquip;
        result.RestraintSetAutoEquip = globalPermsModel.RestraintSetAutoEquip;

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

    public static UserGlobalPermissions ToModelGlobalPerms(this GagspeakAPI.Data.Permissions.UserGlobalPermissions apiGlobalPerms)
    {
        if (apiGlobalPerms == null) return new UserGlobalPermissions();

        UserGlobalPermissions result = new UserGlobalPermissions
        {
            Safeword = apiGlobalPerms.Safeword,
            SafewordUsed = apiGlobalPerms.SafewordUsed,
            CommandsFromFriends = apiGlobalPerms.CommandsFromFriends,
            CommandsFromParty = apiGlobalPerms.CommandsFromParty,
            LiveChatGarblerActive = apiGlobalPerms.LiveChatGarblerActive,
            LiveChatGarblerLocked = apiGlobalPerms.LiveChatGarblerLocked,
            WardrobeEnabled = apiGlobalPerms.WardrobeEnabled,
            ItemAutoEquip = apiGlobalPerms.ItemAutoEquip,
            RestraintSetAutoEquip = apiGlobalPerms.RestraintSetAutoEquip,
            PuppeteerEnabled = apiGlobalPerms.PuppeteerEnabled,
            GlobalTriggerPhrase = apiGlobalPerms.GlobalTriggerPhrase,
            GlobalAllowSitRequests = apiGlobalPerms.GlobalAllowSitRequests,
            GlobalAllowMotionRequests = apiGlobalPerms.GlobalAllowMotionRequests,
            GlobalAllowAllRequests = apiGlobalPerms.GlobalAllowAllRequests,
            MoodlesEnabled = apiGlobalPerms.MoodlesEnabled,
            ToyboxEnabled = apiGlobalPerms.ToyboxEnabled,
            LockToyboxUI = apiGlobalPerms.LockToyboxUI,
            ToyIsActive = apiGlobalPerms.ToyIsActive,
            ToyIntensity = apiGlobalPerms.ToyIntensity,
            SpatialVibratorAudio = apiGlobalPerms.SpatialVibratorAudio
        };

        return result;
    }
    #endregion GlobalPermissionMigrations

    #region PairPermissionMigrations
    public static GagspeakAPI.Data.Permissions.UserPairPermissions ToApiUserPairPerms(this ClientPairPermissions? clientPairPermsModel)
    {
        // if null, return a fresh UserPermissionsComposite object.
        if (clientPairPermsModel == null) return new GagspeakAPI.Data.Permissions.UserPairPermissions();

        // create a new UserPermissionsComposite object
        GagspeakAPI.Data.Permissions.UserPairPermissions result = new GagspeakAPI.Data.Permissions.UserPairPermissions();

        // set the variables
        result.IsPaused = clientPairPermsModel.IsPaused;
        result.GagFeatures = clientPairPermsModel.GagFeatures;
        result.OwnerLocks = clientPairPermsModel.OwnerLocks;
        result.ExtendedLockTimes = clientPairPermsModel.ExtendedLockTimes;
        result.MaxLockTime = clientPairPermsModel.MaxLockTime;
        result.InHardcore = clientPairPermsModel.InHardcore;

        result.ApplyRestraintSets = clientPairPermsModel.ApplyRestraintSets;
        result.LockRestraintSets = clientPairPermsModel.LockRestraintSets;
        result.MaxAllowedRestraintTime = clientPairPermsModel.MaxAllowedRestraintTime;
        result.UnlockRestraintSets = clientPairPermsModel.UnlockRestraintSets;
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
        result.AllowRemovingMoodles = clientPairPermsModel.AllowRemovingMoodles;

        result.ChangeToyState = clientPairPermsModel.ChangeToyState;
        result.CanControlIntensity = clientPairPermsModel.CanControlIntensity;
        result.VibratorAlarms = clientPairPermsModel.VibratorAlarms;
        result.VibratorAlarmsToggle = clientPairPermsModel.VibratorAlarmsToggle;
        result.CanUseRealtimeVibeRemote = clientPairPermsModel.CanUseRealtimeVibeRemote;
        result.CanExecutePatterns = clientPairPermsModel.CanExecutePatterns;
        result.CanExecuteTriggers = clientPairPermsModel.CanExecuteTriggers;
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

    public static ClientPairPermissions ToModelUserPairPerms(this GagspeakAPI.Data.Permissions.UserPairPermissions apiPairPerms)
    {
        if (apiPairPerms == null) return new ClientPairPermissions();

        ClientPairPermissions result = new ClientPairPermissions
        {
            IsPaused = apiPairPerms.IsPaused,
            GagFeatures = apiPairPerms.GagFeatures,
            OwnerLocks = apiPairPerms.OwnerLocks,
            ExtendedLockTimes = apiPairPerms.ExtendedLockTimes,
            MaxLockTime = apiPairPerms.MaxLockTime,
            InHardcore = apiPairPerms.InHardcore,
            ApplyRestraintSets = apiPairPerms.ApplyRestraintSets,
            LockRestraintSets = apiPairPerms.LockRestraintSets,
            MaxAllowedRestraintTime = apiPairPerms.MaxAllowedRestraintTime,
            UnlockRestraintSets = apiPairPerms.UnlockRestraintSets,
            RemoveRestraintSets = apiPairPerms.RemoveRestraintSets,
            TriggerPhrase = apiPairPerms.TriggerPhrase,
            StartChar = apiPairPerms.StartChar,
            EndChar = apiPairPerms.EndChar,
            AllowSitRequests = apiPairPerms.AllowSitRequests,
            AllowMotionRequests = apiPairPerms.AllowMotionRequests,
            AllowAllRequests = apiPairPerms.AllowAllRequests,
            AllowPositiveStatusTypes = apiPairPerms.AllowPositiveStatusTypes,
            AllowNegativeStatusTypes = apiPairPerms.AllowNegativeStatusTypes,
            AllowSpecialStatusTypes = apiPairPerms.AllowSpecialStatusTypes,
            PairCanApplyOwnMoodlesToYou = apiPairPerms.PairCanApplyOwnMoodlesToYou,
            PairCanApplyYourMoodlesToYou = apiPairPerms.PairCanApplyYourMoodlesToYou,
            MaxMoodleTime = apiPairPerms.MaxMoodleTime,
            AllowPermanentMoodles = apiPairPerms.AllowPermanentMoodles,
            AllowRemovingMoodles = apiPairPerms.AllowRemovingMoodles,
            ChangeToyState = apiPairPerms.ChangeToyState,
            CanControlIntensity = apiPairPerms.CanControlIntensity,
            VibratorAlarms = apiPairPerms.VibratorAlarms,
            VibratorAlarmsToggle = apiPairPerms.VibratorAlarmsToggle,
            CanUseRealtimeVibeRemote = apiPairPerms.CanUseRealtimeVibeRemote,
            CanExecutePatterns = apiPairPerms.CanExecutePatterns,
            CanExecuteTriggers = apiPairPerms.CanExecuteTriggers,
            CanSendTriggers = apiPairPerms.CanSendTriggers,
            AllowForcedFollow = apiPairPerms.AllowForcedFollow,
            IsForcedToFollow = apiPairPerms.IsForcedToFollow,
            AllowForcedSit = apiPairPerms.AllowForcedSit,
            IsForcedToSit = apiPairPerms.IsForcedToSit,
            AllowForcedToStay = apiPairPerms.AllowForcedToStay,
            IsForcedToStay = apiPairPerms.IsForcedToStay,
            AllowBlindfold = apiPairPerms.AllowBlindfold,
            ForceLockFirstPerson = apiPairPerms.ForceLockFirstPerson,
            IsBlindfolded = apiPairPerms.IsBlindfolded
        };

        return result;
    }
    #endregion PairPermissionMigrations

    #region PairPermissionAccessMigrations
    public static GagspeakAPI.Data.Permissions.UserEditAccessPermissions ToApiUserPairEditAccessPerms(this ClientPairPermissionAccess pairAccessPermsModel)
    {
        if (pairAccessPermsModel == null) return new GagspeakAPI.Data.Permissions.UserEditAccessPermissions();

        // create new UserPermissionsEditAccessComposite object
        GagspeakAPI.Data.Permissions.UserEditAccessPermissions result = new GagspeakAPI.Data.Permissions.UserEditAccessPermissions();

        // set all access perms to defaults
        result.CommandsFromFriendsAllowed = pairAccessPermsModel.CommandsFromFriendsAllowed;
        result.CommandsFromPartyAllowed = pairAccessPermsModel.CommandsFromPartyAllowed;
        result.LiveChatGarblerActiveAllowed = pairAccessPermsModel.LiveChatGarblerActiveAllowed;
        result.LiveChatGarblerLockedAllowed = pairAccessPermsModel.LiveChatGarblerLockedAllowed;
        result.GagFeaturesAllowed = pairAccessPermsModel.GagFeaturesAllowed;
        result.OwnerLocksAllowed = pairAccessPermsModel.OwnerLocksAllowed;
        result.ExtendedLockTimesAllowed = pairAccessPermsModel.ExtendedLockTimesAllowed;
        result.MaxLockTimeAllowed = pairAccessPermsModel.MaxLockTimeAllowed;

        result.WardrobeEnabledAllowed = pairAccessPermsModel.WardrobeEnabledAllowed;
        result.ItemAutoEquipAllowed = pairAccessPermsModel.ItemAutoEquipAllowed;
        result.RestraintSetAutoEquipAllowed = pairAccessPermsModel.RestraintSetAutoEquipAllowed;
        result.ApplyRestraintSetsAllowed = pairAccessPermsModel.ApplyRestraintSetsAllowed;
        result.LockRestraintSetsAllowed = pairAccessPermsModel.LockRestraintSetsAllowed;
        result.MaxAllowedRestraintTimeAllowed = pairAccessPermsModel.MaxAllowedRestraintTimeAllowed;
        result.UnlockRestraintSetsAllowed = pairAccessPermsModel.UnlockRestraintSetsAllowed;
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
        result.AllowRemovingMoodlesAllowed = pairAccessPermsModel.AllowRemovingMoodlesAllowed;

        result.ToyboxEnabledAllowed = pairAccessPermsModel.ToyboxEnabledAllowed;
        result.LockToyboxUIAllowed = pairAccessPermsModel.LockToyboxUIAllowed;
        result.ToyIsActiveAllowed = pairAccessPermsModel.ToyIsActiveAllowed;
        result.SpatialVibratorAudioAllowed = pairAccessPermsModel.SpatialVibratorAudioAllowed;
        result.ChangeToyStateAllowed = pairAccessPermsModel.ChangeToyStateAllowed;
        result.CanControlIntensityAllowed = pairAccessPermsModel.CanControlIntensityAllowed;
        result.VibratorAlarmsAllowed = pairAccessPermsModel.VibratorAlarmsAllowed;
        result.VibratorAlarmsToggleAllowed = pairAccessPermsModel.VibratorAlarmsToggleAllowed;
        result.CanUseRealtimeVibeRemoteAllowed = pairAccessPermsModel.CanUseRealtimeVibeRemoteAllowed;
        result.CanExecutePatternsAllowed = pairAccessPermsModel.CanExecutePatternsAllowed;
        result.CanExecuteTriggersAllowed = pairAccessPermsModel.CanExecuteTriggersAllowed;
        result.CanSendTriggersAllowed = pairAccessPermsModel.CanSendTriggersAllowed;

        return result;
    }

    public static ClientPairPermissionAccess ToModelUserPairEditAccessPerms(this GagspeakAPI.Data.Permissions.UserEditAccessPermissions apiPairAccessPerms)
    {
        if (apiPairAccessPerms == null) return new ClientPairPermissionAccess();

        ClientPairPermissionAccess result = new ClientPairPermissionAccess
        {
            CommandsFromFriendsAllowed = apiPairAccessPerms.CommandsFromFriendsAllowed,
            CommandsFromPartyAllowed = apiPairAccessPerms.CommandsFromPartyAllowed,
            LiveChatGarblerActiveAllowed = apiPairAccessPerms.LiveChatGarblerActiveAllowed,
            LiveChatGarblerLockedAllowed = apiPairAccessPerms.LiveChatGarblerLockedAllowed,
            GagFeaturesAllowed = apiPairAccessPerms.GagFeaturesAllowed,
            OwnerLocksAllowed = apiPairAccessPerms.OwnerLocksAllowed,
            ExtendedLockTimesAllowed = apiPairAccessPerms.ExtendedLockTimesAllowed,
            MaxLockTimeAllowed = apiPairAccessPerms.MaxLockTimeAllowed,
            WardrobeEnabledAllowed = apiPairAccessPerms.WardrobeEnabledAllowed,
            ItemAutoEquipAllowed = apiPairAccessPerms.ItemAutoEquipAllowed,
            RestraintSetAutoEquipAllowed = apiPairAccessPerms.RestraintSetAutoEquipAllowed,
            ApplyRestraintSetsAllowed = apiPairAccessPerms.ApplyRestraintSetsAllowed,
            LockRestraintSetsAllowed = apiPairAccessPerms.LockRestraintSetsAllowed,
            MaxAllowedRestraintTimeAllowed = apiPairAccessPerms.MaxAllowedRestraintTimeAllowed,
            UnlockRestraintSetsAllowed = apiPairAccessPerms.UnlockRestraintSetsAllowed,
            RemoveRestraintSetsAllowed = apiPairAccessPerms.RemoveRestraintSetsAllowed,
            PuppeteerEnabledAllowed = apiPairAccessPerms.PuppeteerEnabledAllowed,
            AllowSitRequestsAllowed = apiPairAccessPerms.AllowSitRequestsAllowed,
            AllowMotionRequestsAllowed = apiPairAccessPerms.AllowMotionRequestsAllowed,
            AllowAllRequestsAllowed = apiPairAccessPerms.AllowAllRequestsAllowed,
            MoodlesEnabledAllowed = apiPairAccessPerms.MoodlesEnabledAllowed,
            AllowPositiveStatusTypesAllowed = apiPairAccessPerms.AllowPositiveStatusTypesAllowed,
            AllowNegativeStatusTypesAllowed = apiPairAccessPerms.AllowNegativeStatusTypesAllowed,
            AllowSpecialStatusTypesAllowed = apiPairAccessPerms.AllowSpecialStatusTypesAllowed,
            PairCanApplyOwnMoodlesToYouAllowed = apiPairAccessPerms.PairCanApplyOwnMoodlesToYouAllowed,
            PairCanApplyYourMoodlesToYouAllowed = apiPairAccessPerms.PairCanApplyYourMoodlesToYouAllowed,
            MaxMoodleTimeAllowed = apiPairAccessPerms.MaxMoodleTimeAllowed,
            AllowPermanentMoodlesAllowed = apiPairAccessPerms.AllowPermanentMoodlesAllowed,
            AllowRemovingMoodlesAllowed = apiPairAccessPerms.AllowRemovingMoodlesAllowed,
            ToyboxEnabledAllowed = apiPairAccessPerms.ToyboxEnabledAllowed,
            LockToyboxUIAllowed = apiPairAccessPerms.LockToyboxUIAllowed,
            ToyIsActiveAllowed = apiPairAccessPerms.ToyIsActiveAllowed,
            SpatialVibratorAudioAllowed = apiPairAccessPerms.SpatialVibratorAudioAllowed,
            ChangeToyStateAllowed = apiPairAccessPerms.ChangeToyStateAllowed,
            CanControlIntensityAllowed = apiPairAccessPerms.CanControlIntensityAllowed,
            VibratorAlarmsAllowed = apiPairAccessPerms.VibratorAlarmsAllowed,
            VibratorAlarmsToggleAllowed = apiPairAccessPerms.VibratorAlarmsToggleAllowed,
            CanUseRealtimeVibeRemoteAllowed = apiPairAccessPerms.CanUseRealtimeVibeRemoteAllowed,
            CanExecutePatternsAllowed = apiPairAccessPerms.CanExecutePatternsAllowed,
            CanExecuteTriggersAllowed = apiPairAccessPerms.CanExecuteTriggersAllowed,
            CanSendTriggersAllowed = apiPairAccessPerms.CanSendTriggersAllowed
        };

        return result;
    }
    #endregion PairPermissionAccessMigrations
}
#pragma warning restore MA0051; // Method is too long