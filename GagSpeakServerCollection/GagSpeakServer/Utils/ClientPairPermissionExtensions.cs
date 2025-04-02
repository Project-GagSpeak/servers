using GagspeakAPI.Data.Character;
using GagspeakShared.Models;

namespace GagspeakServer.Utils;
/// <summary>
/// Extension helper method for converting ClientPairPermissions to UserPermissionsComposite. for DTO's
/// </summary>
#pragma warning disable MA0051 // Method is too long
public static class ClientPairPermissionExtensions
{
    #region CacheDataMigrations
    public static ActiveGagSlot ToApiGagSlot(this UserGagData gagData)
    {
        return new ActiveGagSlot
        {
            GagItem = gagData.Gag,
            Enabler = gagData.Enabler,
            Padlock = gagData.Padlock,
            Password = gagData.Password,
            Timer = gagData.Timer,
            PadlockAssigner = gagData.PadlockAssigner
        };
    }

    public static ActiveRestriction ToApiRestrictionSlot(this UserRestrictionData restrictionSlot)
    {
        return new ActiveRestriction
        {
            Identifier = restrictionSlot.Identifier,
            Enabler = restrictionSlot.Enabler,
            Padlock = restrictionSlot.Padlock,
            Password = restrictionSlot.Password,
            Timer = restrictionSlot.Timer,
            PadlockAssigner = restrictionSlot.PadlockAssigner
        };
    }

    public static CharaActiveRestraint ToApiRestraintData(this UserRestraintData restraintSet)
    {
        return new CharaActiveRestraint
        {
            Identifier = restraintSet.Identifier,
            LayersBitfield = restraintSet.LayersBitfield,
            Enabler = restraintSet.Enabler,
            Padlock = restraintSet.Padlock,
            Password = restraintSet.Password,
            Timer = restraintSet.Timer,
            PadlockAssigner = restraintSet.PadlockAssigner
        };
    }
    #endregion CacheDataMigrations

    public static GagspeakAPI.Data.Permissions.UserGlobalPermissions ToApiGlobalPerms(this UserGlobalPermissions? globalPermsModel)
    {
        var result = new GagspeakAPI.Data.Permissions.UserGlobalPermissions();

        if (globalPermsModel is null)
            return result;
        // Otherwise update it.
        result.ChatGarblerChannelsBitfield = globalPermsModel.ChatGarblerChannelsBitfield;
        result.ChatGarblerActive = globalPermsModel.ChatGarblerActive;
        result.ChatGarblerLocked = globalPermsModel.ChatGarblerLocked;

        result.WardrobeEnabled = globalPermsModel.WardrobeEnabled;
        result.GagVisuals = globalPermsModel.GagVisuals;
        result.RestrictionVisuals = globalPermsModel.RestrictionVisuals;
        result.RestraintSetVisuals = globalPermsModel.RestraintSetVisuals;

        result.PuppeteerEnabled = globalPermsModel.PuppeteerEnabled;
        result.TriggerPhrase = globalPermsModel.TriggerPhrase;
        result.PuppetPerms = globalPermsModel.PuppetPerms;

        result.ToyboxEnabled = globalPermsModel.ToyboxEnabled;
        result.LockToyboxUI = globalPermsModel.LockToyboxUI;
        result.ToysAreConnected = globalPermsModel.ToysAreConnected;
        result.ToysAreInUse = globalPermsModel.ToysAreInUse;
        result.SpatialAudio = globalPermsModel.SpatialAudio;

        result.ForcedFollow = globalPermsModel.ForcedFollow;
        result.ForcedEmoteState = globalPermsModel.ForcedEmoteState;
        result.ForcedStay = globalPermsModel.ForcedStay;
        result.ChatBoxesHidden = globalPermsModel.ChatBoxesHidden;
        result.ChatInputHidden = globalPermsModel.ChatInputHidden;
        result.ChatInputBlocked = globalPermsModel.ChatInputBlocked;

        return result;
    }

    public static UserGlobalPermissions ToModelGlobalPerms(this GagspeakAPI.Data.Permissions.UserGlobalPermissions apiPerms, UserGlobalPermissions current)
    {
        if (apiPerms is null)
            return current;

        current.ChatGarblerChannelsBitfield = apiPerms.ChatGarblerChannelsBitfield;
        current.ChatGarblerActive = apiPerms.ChatGarblerActive;
        current.ChatGarblerLocked = apiPerms.ChatGarblerLocked;

        current.WardrobeEnabled = apiPerms.WardrobeEnabled;
        current.GagVisuals = apiPerms.GagVisuals;
        current.RestrictionVisuals = apiPerms.RestrictionVisuals;
        current.RestraintSetVisuals = apiPerms.RestraintSetVisuals;
        current.GagVisuals = apiPerms.GagVisuals;
        current.RestrictionVisuals = apiPerms.RestrictionVisuals;
        current.RestraintSetVisuals = apiPerms.RestraintSetVisuals;

        current.PuppeteerEnabled = apiPerms.PuppeteerEnabled;
        current.TriggerPhrase = apiPerms.TriggerPhrase;
        current.PuppetPerms = apiPerms.PuppetPerms;

        current.ToyboxEnabled = apiPerms.ToyboxEnabled;
        current.LockToyboxUI = apiPerms.LockToyboxUI;
        current.ToysAreConnected = apiPerms.ToysAreConnected;
        current.ToysAreInUse = apiPerms.ToysAreInUse;
        current.SpatialAudio = apiPerms.SpatialAudio;

        current.ForcedFollow = apiPerms.ForcedFollow;
        current.ForcedEmoteState = apiPerms.ForcedEmoteState;
        current.ForcedStay = apiPerms.ForcedStay;
        current.ChatBoxesHidden = apiPerms.ChatBoxesHidden;
        current.ChatInputHidden = apiPerms.ChatInputHidden;
        current.ChatInputBlocked = apiPerms.ChatInputBlocked;

        return current;
    }

    public static GagspeakAPI.Data.Permissions.UserPairPermissions ToApiUserPairPerms(this ClientPairPermissions? clientPairPermsModel)
    {
        var result = new GagspeakAPI.Data.Permissions.UserPairPermissions();
        if (clientPairPermsModel is null)
            return result;
        // Otherwise update it.
        result.IsPaused = clientPairPermsModel.IsPaused;

        result.PermanentLocks = clientPairPermsModel.PermanentLocks;
        result.OwnerLocks = clientPairPermsModel.OwnerLocks;
        result.DevotionalLocks = clientPairPermsModel.DevotionalLocks;

        result.ApplyGags = clientPairPermsModel.ApplyGags;
        result.LockGags = clientPairPermsModel.LockGags;
        result.MaxGagTime = clientPairPermsModel.MaxGagTime;
        result.UnlockGags = clientPairPermsModel.UnlockGags;
        result.RemoveGags = clientPairPermsModel.RemoveGags;

        result.ApplyRestrictions = clientPairPermsModel.ApplyRestrictions;
        result.LockRestrictions = clientPairPermsModel.LockRestrictions;
        result.MaxRestrictionTime = clientPairPermsModel.MaxRestrictionTime;
        result.UnlockRestrictions = clientPairPermsModel.UnlockRestrictions;
        result.RemoveRestrictions = clientPairPermsModel.RemoveRestrictions;

        result.ApplyRestraintSets = clientPairPermsModel.ApplyRestraintSets;
        result.ApplyRestraintLayers = clientPairPermsModel.ApplyRestraintLayers;
        result.LockRestraintSets = clientPairPermsModel.LockRestraintSets;
        result.MaxRestraintTime = clientPairPermsModel.MaxRestraintTime;
        result.UnlockRestraintSets = clientPairPermsModel.UnlockRestraintSets;
        result.RemoveRestraintSets = clientPairPermsModel.RemoveRestraintSets;

        result.TriggerPhrase = clientPairPermsModel.TriggerPhrase;
        result.StartChar = clientPairPermsModel.StartChar;
        result.EndChar = clientPairPermsModel.EndChar;
        result.PuppetPerms = clientPairPermsModel.PuppetPerms;

        result.MoodlePerms = clientPairPermsModel.MoodlePerms;
        result.MaxMoodleTime = clientPairPermsModel.MaxMoodleTime;

        result.ToggleToyState = clientPairPermsModel.ToggleToyState;
        result.RemoteControlAccess = clientPairPermsModel.RemoteControlAccess;
        result.ExecutePatterns = clientPairPermsModel.ExecutePatterns;
        result.StopPatterns = clientPairPermsModel.StopPatterns;
        result.ToggleAlarms = clientPairPermsModel.ToggleAlarms;
        result.ToggleTriggers = clientPairPermsModel.ToggleTriggers;

        result.InHardcore = clientPairPermsModel.InHardcore;
        result.PairLockedStates = clientPairPermsModel.PairLockedStates;
        result.AllowForcedFollow = clientPairPermsModel.AllowForcedFollow;
        result.AllowForcedSit = clientPairPermsModel.AllowForcedSit;
        result.AllowForcedEmote = clientPairPermsModel.AllowForcedEmote;
        result.AllowForcedStay = clientPairPermsModel.AllowForcedStay;
        result.AllowGarbleChannelEditing = clientPairPermsModel.AllowGarbleChannelEditing;
        result.AllowHidingChatBoxes = clientPairPermsModel.AllowHidingChatBoxes;
        result.AllowHidingChatInput = clientPairPermsModel.AllowHidingChatInput;
        result.AllowChatInputBlocking = clientPairPermsModel.AllowChatInputBlocking;

        result.PiShockShareCode = clientPairPermsModel.PiShockShareCode;
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
        if (apiPairPerms is null)
            return currentModelPerms;

        currentModelPerms.IsPaused = apiPairPerms.IsPaused;

        currentModelPerms.PermanentLocks = apiPairPerms.PermanentLocks;
        currentModelPerms.OwnerLocks = apiPairPerms.OwnerLocks;
        currentModelPerms.DevotionalLocks = apiPairPerms.DevotionalLocks;

        currentModelPerms.ApplyGags = apiPairPerms.ApplyGags;
        currentModelPerms.LockGags = apiPairPerms.LockGags;
        currentModelPerms.MaxGagTime = apiPairPerms.MaxGagTime;
        currentModelPerms.UnlockGags = apiPairPerms.UnlockGags;
        currentModelPerms.RemoveGags = apiPairPerms.RemoveGags;

        currentModelPerms.ApplyRestrictions = apiPairPerms.ApplyRestrictions;
        currentModelPerms.LockRestrictions = apiPairPerms.LockRestrictions;
        currentModelPerms.MaxRestrictionTime = apiPairPerms.MaxRestrictionTime;
        currentModelPerms.UnlockRestrictions = apiPairPerms.UnlockRestrictions;
        currentModelPerms.RemoveRestrictions = apiPairPerms.RemoveRestrictions;

        currentModelPerms.ApplyRestraintSets = apiPairPerms.ApplyRestraintSets;
        currentModelPerms.ApplyRestraintLayers = apiPairPerms.ApplyRestraintLayers;
        currentModelPerms.LockRestraintSets = apiPairPerms.LockRestraintSets;
        currentModelPerms.MaxRestraintTime = apiPairPerms.MaxRestraintTime;
        currentModelPerms.UnlockRestraintSets = apiPairPerms.UnlockRestraintSets;
        currentModelPerms.RemoveRestraintSets = apiPairPerms.RemoveRestraintSets;

        currentModelPerms.TriggerPhrase = apiPairPerms.TriggerPhrase;
        currentModelPerms.StartChar = apiPairPerms.StartChar;
        currentModelPerms.EndChar = apiPairPerms.EndChar;
        currentModelPerms.PuppetPerms = apiPairPerms.PuppetPerms;

        currentModelPerms.MoodlePerms = apiPairPerms.MoodlePerms;
        currentModelPerms.MaxMoodleTime = apiPairPerms.MaxMoodleTime;

        currentModelPerms.ToggleToyState = apiPairPerms.ToggleToyState;
        currentModelPerms.RemoteControlAccess = apiPairPerms.RemoteControlAccess;
        currentModelPerms.ExecutePatterns = apiPairPerms.ExecutePatterns;
        currentModelPerms.StopPatterns = apiPairPerms.StopPatterns;
        currentModelPerms.ToggleAlarms = apiPairPerms.ToggleAlarms;
        currentModelPerms.ToggleTriggers = apiPairPerms.ToggleTriggers;

        currentModelPerms.InHardcore = apiPairPerms.InHardcore;
        currentModelPerms.PairLockedStates = apiPairPerms.PairLockedStates;
        currentModelPerms.AllowForcedFollow = apiPairPerms.AllowForcedFollow;
        currentModelPerms.AllowForcedSit = apiPairPerms.AllowForcedSit;
        currentModelPerms.AllowForcedEmote = apiPairPerms.AllowForcedEmote;
        currentModelPerms.AllowForcedStay = apiPairPerms.AllowForcedStay;
        currentModelPerms.AllowGarbleChannelEditing = apiPairPerms.AllowGarbleChannelEditing;
        currentModelPerms.AllowHidingChatBoxes = apiPairPerms.AllowHidingChatBoxes;
        currentModelPerms.AllowHidingChatInput = apiPairPerms.AllowHidingChatInput;
        currentModelPerms.AllowChatInputBlocking = apiPairPerms.AllowChatInputBlocking;

        currentModelPerms.PiShockShareCode = apiPairPerms.PiShockShareCode;
        currentModelPerms.AllowShocks = apiPairPerms.AllowShocks;
        currentModelPerms.AllowVibrations = apiPairPerms.AllowVibrations;
        currentModelPerms.AllowBeeps = apiPairPerms.AllowBeeps;
        currentModelPerms.MaxIntensity = apiPairPerms.MaxIntensity;
        currentModelPerms.MaxDuration = apiPairPerms.MaxDuration;
        currentModelPerms.MaxVibrateDuration = apiPairPerms.MaxVibrateDuration;

        return currentModelPerms;
    }

    public static GagspeakAPI.Data.Permissions.UserEditAccessPermissions ToApiUserPairEditAccessPerms(this ClientPairPermissionAccess? pairAccessPermsModel)
    {
        var result = new GagspeakAPI.Data.Permissions.UserEditAccessPermissions();
        if (pairAccessPermsModel is null)
            return result;
        // Otherwise update it.
        result.ChatGarblerActiveAllowed = pairAccessPermsModel.ChatGarblerActiveAllowed;
        result.ChatGarblerLockedAllowed = pairAccessPermsModel.ChatGarblerLockedAllowed;

        result.PermanentLocksAllowed = pairAccessPermsModel.PermanentLocksAllowed;
        result.OwnerLocksAllowed = pairAccessPermsModel.OwnerLocksAllowed;
        result.DevotionalLocksAllowed = pairAccessPermsModel.DevotionalLocksAllowed;

        result.ApplyGagsAllowed = pairAccessPermsModel.ApplyGagsAllowed;
        result.LockGagsAllowed = pairAccessPermsModel.LockGagsAllowed;
        result.MaxGagTimeAllowed = pairAccessPermsModel.MaxGagTimeAllowed;
        result.UnlockGagsAllowed = pairAccessPermsModel.UnlockGagsAllowed;
        result.RemoveGagsAllowed = pairAccessPermsModel.RemoveGagsAllowed;

        result.WardrobeEnabledAllowed = pairAccessPermsModel.WardrobeEnabledAllowed;
        result.GagVisualsAllowed = pairAccessPermsModel.GagVisualsAllowed;
        result.RestrictionVisualsAllowed = pairAccessPermsModel.RestrictionVisualsAllowed;
        result.RestraintSetVisualsAllowed = pairAccessPermsModel.RestraintSetVisualsAllowed;

        result.ApplyRestraintSetsAllowed = pairAccessPermsModel.ApplyRestraintSetsAllowed;
        result.ApplyRestraintLayersAllowed = pairAccessPermsModel.ApplyRestraintLayersAllowed;
        result.LockRestraintSetsAllowed = pairAccessPermsModel.LockRestraintSetsAllowed;
        result.MaxRestrictionTimeAllowed = pairAccessPermsModel.MaxRestrictionTimeAllowed;
        result.UnlockRestraintSetsAllowed = pairAccessPermsModel.UnlockRestraintSetsAllowed;
        result.RemoveRestraintSetsAllowed = pairAccessPermsModel.RemoveRestraintSetsAllowed;

        result.PuppeteerEnabledAllowed = pairAccessPermsModel.PuppeteerEnabledAllowed;
        result.PuppetPermsAllowed = pairAccessPermsModel.PuppetPermsAllowed;

        result.MoodlesEnabledAllowed = pairAccessPermsModel.MoodlesEnabledAllowed;
        result.MoodlePermsAllowed = pairAccessPermsModel.MoodlePermsAllowed;
        result.MaxMoodleTimeAllowed = pairAccessPermsModel.MaxMoodleTimeAllowed;

        result.ToyboxEnabledAllowed = pairAccessPermsModel.ToyboxEnabledAllowed;
        result.LockToyboxUIAllowed = pairAccessPermsModel.LockToyboxUIAllowed;
        result.SpatialAudioAllowed = pairAccessPermsModel.SpatialAudioAllowed;
        result.ToggleToyStateAllowed = pairAccessPermsModel.ToggleToyStateAllowed;
        result.RemoteControlAccessAllowed = pairAccessPermsModel.RemoteControlAccessAllowed;
        result.ExecutePatternsAllowed = pairAccessPermsModel.ExecutePatternsAllowed;
        result.StopPatternsAllowed = pairAccessPermsModel.StopPatternsAllowed;
        result.ToggleAlarmsAllowed = pairAccessPermsModel.ToggleAlarmsAllowed;
        result.ToggleTriggersAllowed = pairAccessPermsModel.ToggleTriggersAllowed;

        return result;
    }

    public static ClientPairPermissionAccess ToModelUserPairEditAccessPerms(this GagspeakAPI.Data.Permissions.UserEditAccessPermissions apiPairAccessPerms, ClientPairPermissionAccess currentPermAccess)
    {
        if (apiPairAccessPerms is null)
            return currentPermAccess;
        // Otherwise update it.
        currentPermAccess.ChatGarblerActiveAllowed = apiPairAccessPerms.ChatGarblerActiveAllowed;
        currentPermAccess.ChatGarblerLockedAllowed = apiPairAccessPerms.ChatGarblerLockedAllowed;

        currentPermAccess.WardrobeEnabledAllowed = apiPairAccessPerms.WardrobeEnabledAllowed;
        currentPermAccess.GagVisualsAllowed = apiPairAccessPerms.GagVisualsAllowed;
        currentPermAccess.RestrictionVisualsAllowed = apiPairAccessPerms.RestrictionVisualsAllowed;
        currentPermAccess.RestraintSetVisualsAllowed = apiPairAccessPerms.RestraintSetVisualsAllowed;

        currentPermAccess.PermanentLocksAllowed = apiPairAccessPerms.PermanentLocksAllowed;
        currentPermAccess.OwnerLocksAllowed = apiPairAccessPerms.OwnerLocksAllowed;
        currentPermAccess.DevotionalLocksAllowed = apiPairAccessPerms.DevotionalLocksAllowed;

        currentPermAccess.ApplyGagsAllowed = apiPairAccessPerms.ApplyGagsAllowed;
        currentPermAccess.LockGagsAllowed = apiPairAccessPerms.LockGagsAllowed;
        currentPermAccess.MaxGagTimeAllowed = apiPairAccessPerms.MaxGagTimeAllowed;
        currentPermAccess.UnlockGagsAllowed = apiPairAccessPerms.UnlockGagsAllowed;
        currentPermAccess.RemoveGagsAllowed = apiPairAccessPerms.RemoveGagsAllowed;

        currentPermAccess.ApplyRestrictionsAllowed = apiPairAccessPerms.ApplyRestrictionsAllowed;
        currentPermAccess.LockRestrictionsAllowed = apiPairAccessPerms.LockRestrictionsAllowed;
        currentPermAccess.MaxRestrictionTimeAllowed = apiPairAccessPerms.MaxRestrictionTimeAllowed;
        currentPermAccess.UnlockRestrictionsAllowed = apiPairAccessPerms.UnlockRestrictionsAllowed;
        currentPermAccess.RemoveRestrictionsAllowed = apiPairAccessPerms.RemoveRestrictionsAllowed;

        currentPermAccess.ApplyRestraintSetsAllowed = apiPairAccessPerms.ApplyRestraintSetsAllowed;
        currentPermAccess.ApplyRestraintLayersAllowed = apiPairAccessPerms.ApplyRestraintLayersAllowed;
        currentPermAccess.LockRestraintSetsAllowed = apiPairAccessPerms.LockRestraintSetsAllowed;
        currentPermAccess.MaxRestrictionTimeAllowed = apiPairAccessPerms.MaxRestrictionTimeAllowed;
        currentPermAccess.UnlockRestraintSetsAllowed = apiPairAccessPerms.UnlockRestraintSetsAllowed;
        currentPermAccess.RemoveRestraintSetsAllowed = apiPairAccessPerms.RemoveRestraintSetsAllowed;

        currentPermAccess.PuppeteerEnabledAllowed = apiPairAccessPerms.PuppeteerEnabledAllowed;
        currentPermAccess.PuppetPermsAllowed = apiPairAccessPerms.PuppetPermsAllowed;

        currentPermAccess.MoodlesEnabledAllowed = apiPairAccessPerms.MoodlesEnabledAllowed;
        currentPermAccess.MoodlePermsAllowed = apiPairAccessPerms.MoodlePermsAllowed;
        currentPermAccess.MaxMoodleTimeAllowed = apiPairAccessPerms.MaxMoodleTimeAllowed;

        currentPermAccess.ToyboxEnabledAllowed = apiPairAccessPerms.ToyboxEnabledAllowed;
        currentPermAccess.LockToyboxUIAllowed = apiPairAccessPerms.LockToyboxUIAllowed;
        currentPermAccess.SpatialAudioAllowed = apiPairAccessPerms.SpatialAudioAllowed;
        currentPermAccess.ToggleToyStateAllowed = apiPairAccessPerms.ToggleToyStateAllowed;
        currentPermAccess.RemoteControlAccessAllowed = apiPairAccessPerms.RemoteControlAccessAllowed;
        currentPermAccess.ExecutePatternsAllowed = apiPairAccessPerms.ExecutePatternsAllowed;
        currentPermAccess.StopPatternsAllowed = apiPairAccessPerms.StopPatternsAllowed;
        currentPermAccess.ToggleAlarmsAllowed = apiPairAccessPerms.ToggleAlarmsAllowed;
        currentPermAccess.ToggleTriggersAllowed = apiPairAccessPerms.ToggleTriggersAllowed;

        return currentPermAccess;
    }
}
#pragma warning restore MA0051