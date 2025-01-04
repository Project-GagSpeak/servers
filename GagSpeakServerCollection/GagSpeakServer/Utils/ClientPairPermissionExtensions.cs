using GagspeakShared.Models;

namespace GagspeakServer.Utils;
/// <summary>
/// Extension helper method for converting ClientPairPermissions to UserPermissionsComposite. for DTO's
/// </summary>
#pragma warning disable MA0051 // Method is too long
public static class ClientPairPermissionExtensions
{
    #region AppearanceDataMigrations
    public static GagspeakAPI.Data.Character.CharaAppearanceData ToApiAppearanceData(this UserGagAppearanceData? appearanceDataModel)
    {
        if (appearanceDataModel == null) return new GagspeakAPI.Data.Character.CharaAppearanceData();

        GagspeakAPI.Data.Character.CharaAppearanceData result = new GagspeakAPI.Data.Character.CharaAppearanceData();

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
    public static GagspeakAPI.Data.Character.CharaActiveStateData ToApiActiveStateData(this UserActiveStateData? activeStateDataModel)
    {
        if (activeStateDataModel == null) return new GagspeakAPI.Data.Character.CharaActiveStateData();

        GagspeakAPI.Data.Character.CharaActiveStateData result = new GagspeakAPI.Data.Character.CharaActiveStateData();

        result.ActiveSetId = activeStateDataModel.ActiveSetId;
        result.ActiveSetEnabler = activeStateDataModel.ActiveSetEnabler;
        result.Padlock = activeStateDataModel.ActiveSetPadLock;
        result.Password = activeStateDataModel.ActiveSetPassword;
        result.Timer = activeStateDataModel.ActiveSetLockTime;
        result.Assigner = activeStateDataModel.ActiveSetLockAssigner;

        return result;
    }
    #endregion ActiveStateDataMigrations


    #region GlobalPermissionMigrations
    public static GagspeakAPI.Data.Permissions.UserGlobalPermissions ToApiGlobalPerms(this UserGlobalPermissions? globalPermsModel)
    {
        if (globalPermsModel == null) return new GagspeakAPI.Data.Permissions.UserGlobalPermissions();

        GagspeakAPI.Data.Permissions.UserGlobalPermissions result = new GagspeakAPI.Data.Permissions.UserGlobalPermissions();

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
        result.GlobalAllowAliasRequests = globalPermsModel.GlobalAllowAliasRequests;

        result.MoodlesEnabled = globalPermsModel.MoodlesEnabled;

        result.ToyboxEnabled = globalPermsModel.ToyboxEnabled;
        result.LockToyboxUI = globalPermsModel.LockToyboxUI;
        result.ToyIsActive = globalPermsModel.ToyIsActive;
        result.SpatialVibratorAudio = globalPermsModel.SpatialVibratorAudio;

        result.ForcedFollow = globalPermsModel.ForcedFollow;
        result.ForcedEmoteState = globalPermsModel.ForcedEmoteState;
        result.ForcedStay = globalPermsModel.ForcedStay;
        result.ForcedBlindfold = globalPermsModel.ForcedBlindfold;
        result.ChatBoxesHidden = globalPermsModel.ChatBoxesHidden;
        result.ChatInputHidden = globalPermsModel.ChatInputHidden;
        result.ChatInputBlocked = globalPermsModel.ChatInputBlocked;

        result.GlobalShockShareCode = globalPermsModel.GlobalShockShareCode;
        result.AllowShocks = globalPermsModel.AllowShocks;
        result.AllowVibrations = globalPermsModel.AllowVibrations;
        result.AllowBeeps = globalPermsModel.AllowBeeps;
        result.MaxIntensity = globalPermsModel.MaxIntensity;
        result.MaxDuration = globalPermsModel.MaxDuration;
        result.GlobalShockVibrateDuration = globalPermsModel.GlobalShockVibrateDuration;

        return result;
    }

    public static UserGlobalPermissions ToModelGlobalPerms(this GagspeakAPI.Data.Permissions.UserGlobalPermissions apiPerms, UserGlobalPermissions current)
    {
        if (apiPerms == null) return current;

        current.LiveChatGarblerActive = apiPerms.LiveChatGarblerActive;
        current.LiveChatGarblerLocked = apiPerms.LiveChatGarblerLocked;

        current.WardrobeEnabled = apiPerms.WardrobeEnabled;
        current.ItemAutoEquip = apiPerms.ItemAutoEquip;
        current.RestraintSetAutoEquip = apiPerms.RestraintSetAutoEquip;

        current.PuppeteerEnabled = apiPerms.PuppeteerEnabled;
        current.GlobalTriggerPhrase = apiPerms.GlobalTriggerPhrase;
        current.GlobalAllowSitRequests = apiPerms.GlobalAllowSitRequests;
        current.GlobalAllowMotionRequests = apiPerms.GlobalAllowMotionRequests;
        current.GlobalAllowAllRequests = apiPerms.GlobalAllowAllRequests;
        current.GlobalAllowAliasRequests = apiPerms.GlobalAllowAliasRequests;

        current.MoodlesEnabled = apiPerms.MoodlesEnabled;

        current.ToyboxEnabled = apiPerms.ToyboxEnabled;
        current.LockToyboxUI = apiPerms.LockToyboxUI;
        current.ToyIsActive = apiPerms.ToyIsActive;
        current.SpatialVibratorAudio = apiPerms.SpatialVibratorAudio;

        current.ForcedFollow = apiPerms.ForcedFollow;
        current.ForcedEmoteState = apiPerms.ForcedEmoteState;
        current.ForcedStay = apiPerms.ForcedStay;
        current.ForcedBlindfold = apiPerms.ForcedBlindfold;
        current.ChatBoxesHidden = apiPerms.ChatBoxesHidden;
        current.ChatInputHidden = apiPerms.ChatInputHidden;
        current.ChatInputBlocked = apiPerms.ChatInputBlocked;

        current.GlobalShockShareCode = apiPerms.GlobalShockShareCode;
        current.AllowShocks = apiPerms.AllowShocks;
        current.AllowVibrations = apiPerms.AllowVibrations;
        current.AllowBeeps = apiPerms.AllowBeeps;
        current.MaxIntensity = apiPerms.MaxIntensity;
        current.MaxDuration = apiPerms.MaxDuration;
        current.GlobalShockVibrateDuration = apiPerms.GlobalShockVibrateDuration;

        return current;
    }
    #endregion GlobalPermissionMigrations

    #region PairPermissionMigrations
    public static GagspeakAPI.Data.Permissions.UserPairPermissions ToApiUserPairPerms(this ClientPairPermissions? clientPairPermsModel)
    {
        // if null, return a fresh UserPermissionsComposite object.
        if (clientPairPermsModel == null) return new GagspeakAPI.Data.Permissions.UserPairPermissions();

        // create a new UserPermissionsComposite object
        GagspeakAPI.Data.Permissions.UserPairPermissions result = new GagspeakAPI.Data.Permissions.UserPairPermissions();

        result.IsPaused = clientPairPermsModel.IsPaused;

        result.PermanentLocks = clientPairPermsModel.PermanentLocks;
        result.OwnerLocks = clientPairPermsModel.OwnerLocks;
        result.DevotionalLocks = clientPairPermsModel.DevotionalLocks;

        result.ApplyGags = clientPairPermsModel.ApplyGags;
        result.LockGags = clientPairPermsModel.LockGags;
        result.MaxGagTime = clientPairPermsModel.MaxGagTime;
        result.UnlockGags = clientPairPermsModel.UnlockGags;
        result.RemoveGags = clientPairPermsModel.RemoveGags;

        result.ApplyRestraintSets = clientPairPermsModel.ApplyRestraintSets;
        result.LockRestraintSets = clientPairPermsModel.LockRestraintSets;
        result.MaxAllowedRestraintTime = clientPairPermsModel.MaxAllowedRestraintTime;
        result.UnlockRestraintSets = clientPairPermsModel.UnlockRestraintSets;
        result.RemoveRestraintSets = clientPairPermsModel.RemoveRestraintSets;

        result.TriggerPhrase = clientPairPermsModel.TriggerPhrase;
        result.StartChar = clientPairPermsModel.StartChar;
        result.EndChar = clientPairPermsModel.EndChar;
        result.SitRequests = clientPairPermsModel.SitRequests;
        result.MotionRequests = clientPairPermsModel.MotionRequests;
        result.AliasRequests = clientPairPermsModel.AliasRequests;
        result.AllRequests = clientPairPermsModel.AllRequests;

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
        result.CanSendAlarms = clientPairPermsModel.CanSendAlarms;
        result.CanExecutePatterns = clientPairPermsModel.CanExecutePatterns;
        result.CanStopPatterns = clientPairPermsModel.CanStopPatterns;
        result.CanToggleTriggers = clientPairPermsModel.CanToggleTriggers;

        result.InHardcore = clientPairPermsModel.InHardcore;
        result.DevotionalStatesForPair = clientPairPermsModel.DevotionalStatesForPair;
        result.AllowForcedFollow = clientPairPermsModel.AllowForcedFollow;
        result.AllowForcedSit = clientPairPermsModel.AllowForcedSit;
        result.AllowForcedEmote = clientPairPermsModel.AllowForcedEmote;
        result.AllowForcedToStay = clientPairPermsModel.AllowForcedToStay;
        result.AllowBlindfold = clientPairPermsModel.AllowBlindfold;
        result.AllowHidingChatBoxes = clientPairPermsModel.AllowHidingChatBoxes;
        result.AllowHidingChatInput = clientPairPermsModel.AllowHidingChatInput;
        result.AllowChatInputBlocking = clientPairPermsModel.AllowChatInputBlocking;

        result.ShockCollarShareCode = clientPairPermsModel.ShockCollarShareCode;
        result.AllowShocks = clientPairPermsModel.AllowShocks;
        result.AllowVibrations = clientPairPermsModel.AllowVibrations;
        result.AllowBeeps = clientPairPermsModel.AllowBeeps;
        result.MaxIntensity = clientPairPermsModel.MaxIntensity;
        result.MaxDuration = clientPairPermsModel.MaxDuration;
        result.MaxVibrateDuration = clientPairPermsModel.MaxVibrateDuration;

        return result;
    }

    public static ClientPairPermissions ToModelUserPairPerms(this GagspeakAPI.Data.Permissions.UserPairPermissions apiPairPerms, ClientPairPermissions currentModelPerms)
    {
        if (apiPairPerms == null) return currentModelPerms;

        currentModelPerms.IsPaused = apiPairPerms.IsPaused;

        currentModelPerms.PermanentLocks = apiPairPerms.PermanentLocks;
        currentModelPerms.OwnerLocks = apiPairPerms.OwnerLocks;
        currentModelPerms.DevotionalLocks = apiPairPerms.DevotionalLocks;

        currentModelPerms.ApplyGags = apiPairPerms.ApplyGags;
        currentModelPerms.LockGags = apiPairPerms.LockGags;
        currentModelPerms.MaxGagTime = apiPairPerms.MaxGagTime;
        currentModelPerms.UnlockGags = apiPairPerms.UnlockGags;
        currentModelPerms.RemoveGags = apiPairPerms.RemoveGags;

        currentModelPerms.ApplyRestraintSets = apiPairPerms.ApplyRestraintSets;
        currentModelPerms.LockRestraintSets = apiPairPerms.LockRestraintSets;
        currentModelPerms.MaxAllowedRestraintTime = apiPairPerms.MaxAllowedRestraintTime;
        currentModelPerms.UnlockRestraintSets = apiPairPerms.UnlockRestraintSets;
        currentModelPerms.RemoveRestraintSets = apiPairPerms.RemoveRestraintSets;

        currentModelPerms.TriggerPhrase = apiPairPerms.TriggerPhrase;
        currentModelPerms.StartChar = apiPairPerms.StartChar;
        currentModelPerms.EndChar = apiPairPerms.EndChar;
        currentModelPerms.SitRequests = apiPairPerms.SitRequests;
        currentModelPerms.MotionRequests = apiPairPerms.MotionRequests;
        currentModelPerms.AliasRequests = apiPairPerms.AliasRequests;
        currentModelPerms.AllRequests = apiPairPerms.AllRequests;

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
        currentModelPerms.CanSendAlarms = apiPairPerms.CanSendAlarms;
        currentModelPerms.CanExecutePatterns = apiPairPerms.CanExecutePatterns;
        currentModelPerms.CanStopPatterns = apiPairPerms.CanStopPatterns;
        currentModelPerms.CanToggleTriggers = apiPairPerms.CanToggleTriggers;

        currentModelPerms.InHardcore = apiPairPerms.InHardcore;
        currentModelPerms.DevotionalStatesForPair = apiPairPerms.DevotionalStatesForPair;
        currentModelPerms.AllowForcedFollow = apiPairPerms.AllowForcedFollow;
        currentModelPerms.AllowForcedSit = apiPairPerms.AllowForcedSit;
        currentModelPerms.AllowForcedEmote = apiPairPerms.AllowForcedEmote;
        currentModelPerms.AllowForcedToStay = apiPairPerms.AllowForcedToStay;
        currentModelPerms.AllowBlindfold = apiPairPerms.AllowBlindfold;
        currentModelPerms.AllowHidingChatBoxes = apiPairPerms.AllowHidingChatBoxes;
        currentModelPerms.AllowHidingChatInput = apiPairPerms.AllowHidingChatInput;
        currentModelPerms.AllowChatInputBlocking = apiPairPerms.AllowChatInputBlocking;

        currentModelPerms.ShockCollarShareCode = apiPairPerms.ShockCollarShareCode;
        currentModelPerms.AllowShocks = apiPairPerms.AllowShocks;
        currentModelPerms.AllowVibrations = apiPairPerms.AllowVibrations;
        currentModelPerms.AllowBeeps = apiPairPerms.AllowBeeps;
        currentModelPerms.MaxIntensity = apiPairPerms.MaxIntensity;
        currentModelPerms.MaxDuration = apiPairPerms.MaxDuration;
        currentModelPerms.MaxVibrateDuration = apiPairPerms.MaxVibrateDuration;

        return currentModelPerms;
    }
    #endregion PairPermissionMigrations

    #region PairPermissionAccessMigrations
    public static GagspeakAPI.Data.Permissions.UserEditAccessPermissions ToApiUserPairEditAccessPerms(this ClientPairPermissionAccess? pairAccessPermsModel)
    {
        if (pairAccessPermsModel == null) return new GagspeakAPI.Data.Permissions.UserEditAccessPermissions();

        // create new UserPermissionsEditAccessComposite object
        GagspeakAPI.Data.Permissions.UserEditAccessPermissions result = new GagspeakAPI.Data.Permissions.UserEditAccessPermissions();

        // set all access perms to defaults
        result.LiveChatGarblerActiveAllowed = pairAccessPermsModel.LiveChatGarblerActiveAllowed;
        result.LiveChatGarblerLockedAllowed = pairAccessPermsModel.LiveChatGarblerLockedAllowed;

        result.PermanentLocksAllowed = pairAccessPermsModel.PermanentLocksAllowed;
        result.OwnerLocksAllowed = pairAccessPermsModel.OwnerLocksAllowed;
        result.DevotionalLocksAllowed = pairAccessPermsModel.DevotionalLocksAllowed;

        result.ApplyGagsAllowed = pairAccessPermsModel.ApplyGagsAllowed;
        result.LockGagsAllowed = pairAccessPermsModel.LockGagsAllowed;
        result.MaxGagTimeAllowed = pairAccessPermsModel.MaxGagTimeAllowed;
        result.UnlockGagsAllowed = pairAccessPermsModel.UnlockGagsAllowed;
        result.RemoveGagsAllowed = pairAccessPermsModel.RemoveGagsAllowed;

        result.WardrobeEnabledAllowed = pairAccessPermsModel.WardrobeEnabledAllowed;
        result.ItemAutoEquipAllowed = pairAccessPermsModel.ItemAutoEquipAllowed;
        result.RestraintSetAutoEquipAllowed = pairAccessPermsModel.RestraintSetAutoEquipAllowed;
        result.ApplyRestraintSetsAllowed = pairAccessPermsModel.ApplyRestraintSetsAllowed;
        result.LockRestraintSetsAllowed = pairAccessPermsModel.LockRestraintSetsAllowed;
        result.MaxAllowedRestraintTimeAllowed = pairAccessPermsModel.MaxAllowedRestraintTimeAllowed;
        result.UnlockRestraintSetsAllowed = pairAccessPermsModel.UnlockRestraintSetsAllowed;
        result.RemoveRestraintSetsAllowed = pairAccessPermsModel.RemoveRestraintSetsAllowed;

        result.PuppeteerEnabledAllowed = pairAccessPermsModel.PuppeteerEnabledAllowed;
        result.SitRequestsAllowed = pairAccessPermsModel.SitRequestsAllowed;
        result.MotionRequestsAllowed = pairAccessPermsModel.MotionRequestsAllowed;
        result.AliasRequestsAllowed = pairAccessPermsModel.AliasRequestsAllowed;
        result.AllRequestsAllowed = pairAccessPermsModel.AllRequestsAllowed;

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
        result.CanSendAlarmsAllowed = pairAccessPermsModel.CanSendAlarmsAllowed;
        result.CanExecutePatternsAllowed = pairAccessPermsModel.CanExecutePatternsAllowed;
        result.CanStopPatternsAllowed = pairAccessPermsModel.CanStopPatternsAllowed;
        result.CanToggleTriggersAllowed = pairAccessPermsModel.CanToggleTriggersAllowed;

        return result;
    }

    public static ClientPairPermissionAccess ToModelUserPairEditAccessPerms(this GagspeakAPI.Data.Permissions.UserEditAccessPermissions apiPairAccessPerms, ClientPairPermissionAccess currentPermAccess)
    {
        if (apiPairAccessPerms == null) return currentPermAccess;

        currentPermAccess.LiveChatGarblerActiveAllowed = apiPairAccessPerms.LiveChatGarblerActiveAllowed;
        currentPermAccess.LiveChatGarblerLockedAllowed = apiPairAccessPerms.LiveChatGarblerLockedAllowed;

        currentPermAccess.PermanentLocksAllowed = apiPairAccessPerms.PermanentLocksAllowed;
        currentPermAccess.OwnerLocksAllowed = apiPairAccessPerms.OwnerLocksAllowed;
        currentPermAccess.DevotionalLocksAllowed = apiPairAccessPerms.DevotionalLocksAllowed;

        currentPermAccess.ApplyGagsAllowed = apiPairAccessPerms.ApplyGagsAllowed;
        currentPermAccess.LockGagsAllowed = apiPairAccessPerms.LockGagsAllowed;
        currentPermAccess.MaxGagTimeAllowed = apiPairAccessPerms.MaxGagTimeAllowed;
        currentPermAccess.UnlockGagsAllowed = apiPairAccessPerms.UnlockGagsAllowed;
        currentPermAccess.RemoveGagsAllowed = apiPairAccessPerms.RemoveGagsAllowed;

        currentPermAccess.WardrobeEnabledAllowed = apiPairAccessPerms.WardrobeEnabledAllowed;
        currentPermAccess.ItemAutoEquipAllowed = apiPairAccessPerms.ItemAutoEquipAllowed;
        currentPermAccess.RestraintSetAutoEquipAllowed = apiPairAccessPerms.RestraintSetAutoEquipAllowed;
        currentPermAccess.ApplyRestraintSetsAllowed = apiPairAccessPerms.ApplyRestraintSetsAllowed;
        currentPermAccess.LockRestraintSetsAllowed = apiPairAccessPerms.LockRestraintSetsAllowed;
        currentPermAccess.MaxAllowedRestraintTimeAllowed = apiPairAccessPerms.MaxAllowedRestraintTimeAllowed;
        currentPermAccess.UnlockRestraintSetsAllowed = apiPairAccessPerms.UnlockRestraintSetsAllowed;
        currentPermAccess.RemoveRestraintSetsAllowed = apiPairAccessPerms.RemoveRestraintSetsAllowed;

        currentPermAccess.PuppeteerEnabledAllowed = apiPairAccessPerms.PuppeteerEnabledAllowed;
        currentPermAccess.SitRequestsAllowed = apiPairAccessPerms.SitRequestsAllowed;
        currentPermAccess.MotionRequestsAllowed = apiPairAccessPerms.MotionRequestsAllowed;
        currentPermAccess.AliasRequestsAllowed = apiPairAccessPerms.AliasRequestsAllowed;
        currentPermAccess.AllRequestsAllowed = apiPairAccessPerms.AllRequestsAllowed;

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
        currentPermAccess.CanSendAlarmsAllowed = apiPairAccessPerms.CanSendAlarmsAllowed;
        currentPermAccess.CanExecutePatternsAllowed = apiPairAccessPerms.CanExecutePatternsAllowed;
        currentPermAccess.CanStopPatternsAllowed = apiPairAccessPerms.CanStopPatternsAllowed;
        currentPermAccess.CanToggleTriggersAllowed = apiPairAccessPerms.CanToggleTriggersAllowed;

        return currentPermAccess;
    }
    #endregion PairPermissionAccessMigrations
}
#pragma warning restore MA0051 // Method is too long