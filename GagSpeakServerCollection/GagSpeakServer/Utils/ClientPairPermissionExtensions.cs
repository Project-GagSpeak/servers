using GagspeakShared.Models;

namespace GagspeakServer.Utils;
/// <summary>
/// Extention helper method for converting ClientPairPermissions to UserPermissionsComposite. for DTO's
/// </summary>
#pragma warning disable MA0051; // Method is too long
public static class ClientPairPermissionExtensions
{
    #region AppearanceDataMigrations
    public static GagspeakAPI.Data.Character.CharacterAppearanceData ToApiAppearanceData(this UserGagAppearanceData? appearanceDataModel)
    {
        if (appearanceDataModel == null) return new GagspeakAPI.Data.Character.CharacterAppearanceData();

        GagspeakAPI.Data.Character.CharacterAppearanceData result = new GagspeakAPI.Data.Character.CharacterAppearanceData();

        // Assuming the slots are defined in order
        result.GagSlots[0].GagType = appearanceDataModel.SlotOneGagType;
        result.GagSlots[0].Padlock = appearanceDataModel.SlotOneGagPadlock;
        result.GagSlots[0].Password = appearanceDataModel.SlotOneGagPassword;
        result.GagSlots[0].Timer = appearanceDataModel.SlotOneGagTimer;
        result.GagSlots[0].Assigner = appearanceDataModel.SlotOneGagAssigner;

        result.GagSlots[1].GagType = appearanceDataModel.SlotTwoGagType;
        result.GagSlots[1].Padlock = appearanceDataModel.SlotTwoGagPadlock;
        result.GagSlots[1].Password = appearanceDataModel.SlotTwoGagPassword;
        result.GagSlots[1].Timer = appearanceDataModel.SlotTwoGagTimer;
        result.GagSlots[1].Assigner = appearanceDataModel.SlotTwoGagAssigner;

        result.GagSlots[2].GagType = appearanceDataModel.SlotThreeGagType;
        result.GagSlots[2].Padlock = appearanceDataModel.SlotThreeGagPadlock;
        result.GagSlots[2].Password = appearanceDataModel.SlotThreeGagPassword;
        result.GagSlots[2].Timer = appearanceDataModel.SlotThreeGagTimer;
        result.GagSlots[2].Assigner = appearanceDataModel.SlotThreeGagAssigner;

        return result;
    }
    #endregion AppearanceDataMigrations

    #region ActiveStateDataMigrations
    public static GagspeakAPI.Data.Character.CharacterActiveStateData ToApiActiveStateData(this UserActiveStateData? activeStateDataModel)
	{
		if (activeStateDataModel == null) return new GagspeakAPI.Data.Character.CharacterActiveStateData();

		GagspeakAPI.Data.Character.CharacterActiveStateData result = new GagspeakAPI.Data.Character.CharacterActiveStateData();

        result.WardrobeActiveSetName = activeStateDataModel.WardrobeActiveSetName;
        result.WardrobeActiveSetAssigner = activeStateDataModel.WardrobeActiveSetAssigner;
        result.WardrobeActiveSetPadLock = activeStateDataModel.WardrobeActiveSetPadLock;
        result.WardrobeActiveSetPassword = activeStateDataModel.WardrobeActiveSetPassword;
        result.WardrobeActiveSetLockTime = activeStateDataModel.WardrobeActiveSetLockTime;
        result.WardrobeActiveSetLockAssigner = activeStateDataModel.WardrobeActiveSetLockAssigner;
        result.ToyboxActivePatternName = activeStateDataModel.ToyboxActivePatternName;
		return result;
	}
	#endregion ActiveStateDataMigrations


	#region GlobalPermissionMigrations
	public static GagspeakAPI.Data.Permissions.UserGlobalPermissions ToApiGlobalPerms(this UserGlobalPermissions? globalPermsModel)
    {
        if (globalPermsModel == null) return new GagspeakAPI.Data.Permissions.UserGlobalPermissions();

        GagspeakAPI.Data.Permissions.UserGlobalPermissions result = new GagspeakAPI.Data.Permissions.UserGlobalPermissions();

        result.Safeword = globalPermsModel.Safeword;
        result.SafewordUsed = globalPermsModel.SafewordUsed;
        result.HardcoreSafewordUsed = globalPermsModel.HardcoreSafewordUsed;
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
        result.SpatialVibratorAudio = globalPermsModel.SpatialVibratorAudio;

        return result;
    }

    public static UserGlobalPermissions ToModelGlobalPerms(this GagspeakAPI.Data.Permissions.UserGlobalPermissions apiGlobalPerms, UserGlobalPermissions currentModelGlobalPerms)
    {
        if (apiGlobalPerms == null) return currentModelGlobalPerms;

        currentModelGlobalPerms.Safeword = apiGlobalPerms.Safeword;
        currentModelGlobalPerms.SafewordUsed = apiGlobalPerms.SafewordUsed;
        currentModelGlobalPerms.HardcoreSafewordUsed = apiGlobalPerms.HardcoreSafewordUsed;
        currentModelGlobalPerms.LiveChatGarblerActive = apiGlobalPerms.LiveChatGarblerActive;
        currentModelGlobalPerms.LiveChatGarblerLocked = apiGlobalPerms.LiveChatGarblerLocked;
        currentModelGlobalPerms.WardrobeEnabled = apiGlobalPerms.WardrobeEnabled;
        currentModelGlobalPerms.ItemAutoEquip = apiGlobalPerms.ItemAutoEquip;
        currentModelGlobalPerms.RestraintSetAutoEquip = apiGlobalPerms.RestraintSetAutoEquip;
        currentModelGlobalPerms.PuppeteerEnabled = apiGlobalPerms.PuppeteerEnabled;
        currentModelGlobalPerms.GlobalTriggerPhrase = apiGlobalPerms.GlobalTriggerPhrase;
        currentModelGlobalPerms.GlobalAllowSitRequests = apiGlobalPerms.GlobalAllowSitRequests;
        currentModelGlobalPerms.GlobalAllowMotionRequests = apiGlobalPerms.GlobalAllowMotionRequests;
        currentModelGlobalPerms.GlobalAllowAllRequests = apiGlobalPerms.GlobalAllowAllRequests;
        currentModelGlobalPerms.MoodlesEnabled = apiGlobalPerms.MoodlesEnabled;
        currentModelGlobalPerms.ToyboxEnabled = apiGlobalPerms.ToyboxEnabled;
        currentModelGlobalPerms.LockToyboxUI = apiGlobalPerms.LockToyboxUI;
        currentModelGlobalPerms.ToyIsActive = apiGlobalPerms.ToyIsActive;
        currentModelGlobalPerms.SpatialVibratorAudio = apiGlobalPerms.SpatialVibratorAudio;

        return currentModelGlobalPerms;
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

        result.CanToggleToyState = clientPairPermsModel.CanToggleToyState;
        result.CanUseVibeRemote = clientPairPermsModel.CanUseVibeRemote;
        result.CanToggleAlarms = clientPairPermsModel.CanToggleAlarms;
        result.CanExecutePatterns = clientPairPermsModel.CanExecutePatterns;
        result.CanToggleTriggers = clientPairPermsModel.CanToggleTriggers;
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

    public static ClientPairPermissions ToModelUserPairPerms(this GagspeakAPI.Data.Permissions.UserPairPermissions apiPairPerms, ClientPairPermissions currentModelPerms)
    {
        if (apiPairPerms == null) return currentModelPerms;

		currentModelPerms.IsPaused = apiPairPerms.IsPaused;
        currentModelPerms.GagFeatures = apiPairPerms.GagFeatures;
        currentModelPerms.OwnerLocks = apiPairPerms.OwnerLocks;
        currentModelPerms.ExtendedLockTimes = apiPairPerms.ExtendedLockTimes;
        currentModelPerms.MaxLockTime = apiPairPerms.MaxLockTime;
        currentModelPerms.InHardcore = apiPairPerms.InHardcore;

        currentModelPerms.ApplyRestraintSets = apiPairPerms.ApplyRestraintSets;
        currentModelPerms.LockRestraintSets = apiPairPerms.LockRestraintSets;
        currentModelPerms.MaxAllowedRestraintTime = apiPairPerms.MaxAllowedRestraintTime;
        currentModelPerms.UnlockRestraintSets = apiPairPerms.UnlockRestraintSets;
        currentModelPerms.RemoveRestraintSets = apiPairPerms.RemoveRestraintSets;

        currentModelPerms.TriggerPhrase = apiPairPerms.TriggerPhrase;
        currentModelPerms.StartChar = apiPairPerms.StartChar;
        currentModelPerms.EndChar = apiPairPerms.EndChar;
        currentModelPerms.AllowSitRequests = apiPairPerms.AllowSitRequests;
        currentModelPerms.AllowMotionRequests = apiPairPerms.AllowMotionRequests;
        currentModelPerms.AllowAllRequests = apiPairPerms.AllowAllRequests;

        currentModelPerms.AllowPositiveStatusTypes = apiPairPerms.AllowPositiveStatusTypes;
        currentModelPerms.AllowNegativeStatusTypes = apiPairPerms.AllowNegativeStatusTypes;
        currentModelPerms.AllowSpecialStatusTypes = apiPairPerms.AllowSpecialStatusTypes;
        currentModelPerms.PairCanApplyOwnMoodlesToYou = apiPairPerms.PairCanApplyOwnMoodlesToYou;
        currentModelPerms.PairCanApplyYourMoodlesToYou = apiPairPerms.PairCanApplyYourMoodlesToYou;
        currentModelPerms.MaxMoodleTime = apiPairPerms.MaxMoodleTime;
        currentModelPerms.AllowPermanentMoodles = apiPairPerms.AllowPermanentMoodles;
        currentModelPerms.AllowRemovingMoodles = apiPairPerms.AllowRemovingMoodles;

        currentModelPerms.CanToggleToyState = apiPairPerms.CanToggleToyState;
        currentModelPerms.CanUseVibeRemote = apiPairPerms.CanUseVibeRemote;
        currentModelPerms.CanToggleAlarms = apiPairPerms.CanToggleAlarms;
        currentModelPerms.CanExecutePatterns = apiPairPerms.CanExecutePatterns;
        currentModelPerms.CanToggleTriggers = apiPairPerms.CanToggleTriggers;
        currentModelPerms.CanSendTriggers = apiPairPerms.CanSendTriggers;

        currentModelPerms.AllowForcedFollow = apiPairPerms.AllowForcedFollow;
        currentModelPerms.IsForcedToFollow = apiPairPerms.IsForcedToFollow;
        currentModelPerms.AllowForcedSit = apiPairPerms.AllowForcedSit;
        currentModelPerms.IsForcedToSit = apiPairPerms.IsForcedToSit;
        currentModelPerms.AllowForcedToStay = apiPairPerms.AllowForcedToStay;
        currentModelPerms.IsForcedToStay = apiPairPerms.IsForcedToStay;
        currentModelPerms.AllowBlindfold = apiPairPerms.AllowBlindfold;
        currentModelPerms.ForceLockFirstPerson = apiPairPerms.ForceLockFirstPerson;
        currentModelPerms.IsBlindfolded = apiPairPerms.IsBlindfolded;

        return currentModelPerms;
    }
    #endregion PairPermissionMigrations

    #region PairPermissionAccessMigrations
    public static GagspeakAPI.Data.Permissions.UserEditAccessPermissions ToApiUserPairEditAccessPerms(this ClientPairPermissionAccess pairAccessPermsModel)
    {
        if (pairAccessPermsModel == null) return new GagspeakAPI.Data.Permissions.UserEditAccessPermissions();

        // create new UserPermissionsEditAccessComposite object
        GagspeakAPI.Data.Permissions.UserEditAccessPermissions result = new GagspeakAPI.Data.Permissions.UserEditAccessPermissions();

        // set all access perms to defaults
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
        result.SpatialVibratorAudioAllowed = pairAccessPermsModel.SpatialVibratorAudioAllowed;
        result.CanToggleToyStateAllowed = pairAccessPermsModel.CanToggleToyStateAllowed;
        result.CanUseVibeRemoteAllowed = pairAccessPermsModel.CanUseVibeRemoteAllowed;
        result.CanToggleAlarmsAllowed = pairAccessPermsModel.CanToggleAlarmsAllowed;
        result.CanExecutePatternsAllowed = pairAccessPermsModel.CanExecutePatternsAllowed;
        result.CanToggleTriggersAllowed = pairAccessPermsModel.CanToggleTriggersAllowed;
        result.CanSendTriggersAllowed = pairAccessPermsModel.CanSendTriggersAllowed;

        return result;
    }

    public static ClientPairPermissionAccess ToModelUserPairEditAccessPerms(this GagspeakAPI.Data.Permissions.UserEditAccessPermissions apiPairAccessPerms, ClientPairPermissionAccess currentPermAccess)
    {
        if (apiPairAccessPerms == null) return currentPermAccess;

        currentPermAccess.LiveChatGarblerActiveAllowed = apiPairAccessPerms.LiveChatGarblerActiveAllowed;
        currentPermAccess.LiveChatGarblerLockedAllowed = apiPairAccessPerms.LiveChatGarblerLockedAllowed;
        currentPermAccess.GagFeaturesAllowed = apiPairAccessPerms.GagFeaturesAllowed;
        currentPermAccess.OwnerLocksAllowed = apiPairAccessPerms.OwnerLocksAllowed;
        currentPermAccess.ExtendedLockTimesAllowed = apiPairAccessPerms.ExtendedLockTimesAllowed;
        currentPermAccess.MaxLockTimeAllowed = apiPairAccessPerms.MaxLockTimeAllowed;
        currentPermAccess.WardrobeEnabledAllowed = apiPairAccessPerms.WardrobeEnabledAllowed;
        currentPermAccess.ItemAutoEquipAllowed = apiPairAccessPerms.ItemAutoEquipAllowed;
        currentPermAccess.RestraintSetAutoEquipAllowed = apiPairAccessPerms.RestraintSetAutoEquipAllowed;
        currentPermAccess.ApplyRestraintSetsAllowed = apiPairAccessPerms.ApplyRestraintSetsAllowed;
        currentPermAccess.LockRestraintSetsAllowed = apiPairAccessPerms.LockRestraintSetsAllowed;
        currentPermAccess.MaxAllowedRestraintTimeAllowed = apiPairAccessPerms.MaxAllowedRestraintTimeAllowed;
        currentPermAccess.UnlockRestraintSetsAllowed = apiPairAccessPerms.UnlockRestraintSetsAllowed;
        currentPermAccess.RemoveRestraintSetsAllowed = apiPairAccessPerms.RemoveRestraintSetsAllowed;
        currentPermAccess.PuppeteerEnabledAllowed = apiPairAccessPerms.PuppeteerEnabledAllowed;
        currentPermAccess.AllowSitRequestsAllowed = apiPairAccessPerms.AllowSitRequestsAllowed;
        currentPermAccess.AllowMotionRequestsAllowed = apiPairAccessPerms.AllowMotionRequestsAllowed;
        currentPermAccess.AllowAllRequestsAllowed = apiPairAccessPerms.AllowAllRequestsAllowed;
        currentPermAccess.MoodlesEnabledAllowed = apiPairAccessPerms.MoodlesEnabledAllowed;
        currentPermAccess.AllowPositiveStatusTypesAllowed = apiPairAccessPerms.AllowPositiveStatusTypesAllowed;
        currentPermAccess.AllowNegativeStatusTypesAllowed = apiPairAccessPerms.AllowNegativeStatusTypesAllowed;
        currentPermAccess.AllowSpecialStatusTypesAllowed = apiPairAccessPerms.AllowSpecialStatusTypesAllowed;
        currentPermAccess.PairCanApplyOwnMoodlesToYouAllowed = apiPairAccessPerms.PairCanApplyOwnMoodlesToYouAllowed;
        currentPermAccess.PairCanApplyYourMoodlesToYouAllowed = apiPairAccessPerms.PairCanApplyYourMoodlesToYouAllowed;
        currentPermAccess.MaxMoodleTimeAllowed = apiPairAccessPerms.MaxMoodleTimeAllowed;
        currentPermAccess.AllowPermanentMoodlesAllowed = apiPairAccessPerms.AllowPermanentMoodlesAllowed;
        currentPermAccess.AllowRemovingMoodlesAllowed = apiPairAccessPerms.AllowRemovingMoodlesAllowed;
        currentPermAccess.ToyboxEnabledAllowed = apiPairAccessPerms.ToyboxEnabledAllowed;
        currentPermAccess.LockToyboxUIAllowed = apiPairAccessPerms.LockToyboxUIAllowed;
        currentPermAccess.SpatialVibratorAudioAllowed = apiPairAccessPerms.SpatialVibratorAudioAllowed;
        currentPermAccess.CanToggleToyStateAllowed = apiPairAccessPerms.CanToggleToyStateAllowed;
        currentPermAccess.CanUseVibeRemoteAllowed = apiPairAccessPerms.CanUseVibeRemoteAllowed;
        currentPermAccess.CanToggleAlarmsAllowed = apiPairAccessPerms.CanToggleAlarmsAllowed;
        currentPermAccess.CanExecutePatternsAllowed = apiPairAccessPerms.CanExecutePatternsAllowed;
        currentPermAccess.CanToggleTriggersAllowed = apiPairAccessPerms.CanToggleTriggersAllowed;
        currentPermAccess.CanSendTriggersAllowed = apiPairAccessPerms.CanSendTriggersAllowed;

        return currentPermAccess;
    }
    #endregion PairPermissionAccessMigrations
}
#pragma warning restore MA0051; // Method is too long