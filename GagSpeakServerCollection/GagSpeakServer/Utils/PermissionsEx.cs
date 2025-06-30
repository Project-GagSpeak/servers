using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakShared.Models;

namespace GagspeakServer.Utils;

#pragma warning disable MA0051 // Method is too long
#nullable enable
public static class PermissionsEx
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
            ActiveLayers = restraintSet.ActiveLayers,
            Enabler = restraintSet.Enabler,
            Padlock = restraintSet.Padlock,
            Password = restraintSet.Password,
            Timer = restraintSet.Timer,
            PadlockAssigner = restraintSet.PadlockAssigner
        };
    }
    #endregion CacheDataMigrations

    public static GlobalPerms ToApiGlobalPerms(this UserGlobalPermissions? databasePerms)
    {
        var apiPerms = new GlobalPerms();

        if (databasePerms is null)
            return apiPerms;

        apiPerms.AllowedGarblerChannels = databasePerms.AllowedGarblerChannels;
        apiPerms.ChatGarblerActive = databasePerms.ChatGarblerActive;
        apiPerms.ChatGarblerLocked = databasePerms.ChatGarblerLocked;
        apiPerms.GaggedNameplate = databasePerms.GaggedNameplate;

        apiPerms.WardrobeEnabled = databasePerms.WardrobeEnabled;
        apiPerms.GagVisuals = databasePerms.GagVisuals;
        apiPerms.RestrictionVisuals = databasePerms.RestrictionVisuals;
        apiPerms.RestraintSetVisuals = databasePerms.RestraintSetVisuals;

        apiPerms.PuppeteerEnabled = databasePerms.PuppeteerEnabled;
        apiPerms.TriggerPhrase = databasePerms.TriggerPhrase;
        apiPerms.PuppetPerms = databasePerms.PuppetPerms;

        apiPerms.ToyboxEnabled = databasePerms.ToyboxEnabled;
        apiPerms.LockToyboxUI = databasePerms.LockToyboxUI;
        apiPerms.ToysAreConnected = databasePerms.ToysAreConnected;
        apiPerms.ToysAreInUse = databasePerms.ToysAreInUse;
        apiPerms.SpatialAudio = databasePerms.SpatialAudio;

        apiPerms.ForcedFollow = databasePerms.ForcedFollow;
        apiPerms.ForcedEmoteState = databasePerms.ForcedEmoteState;
        apiPerms.ForcedStay = databasePerms.ForcedStay;
        apiPerms.ChatBoxesHidden = databasePerms.ChatBoxesHidden;
        apiPerms.ChatInputHidden = databasePerms.ChatInputHidden;
        apiPerms.ChatInputBlocked = databasePerms.ChatInputBlocked;
        apiPerms.HypnosisCustomEffect = databasePerms.HypnosisCustomEffect;

        apiPerms.GlobalShockShareCode = databasePerms.GlobalShockShareCode;
        apiPerms.AllowShocks = databasePerms.AllowShocks;
        apiPerms.AllowVibrations = databasePerms.AllowVibrations;
        apiPerms.AllowBeeps = databasePerms.AllowBeeps;
        apiPerms.MaxIntensity = databasePerms.MaxIntensity;
        apiPerms.MaxDuration = databasePerms.MaxDuration;
        apiPerms.ShockVibrateDuration = databasePerms.ShockVibrateDuration;

        return apiPerms;
    }

    public static UserGlobalPermissions ToModelGlobalPerms(this GlobalPerms apiPerms, UserGlobalPermissions current)
    {
        if (apiPerms is null)
            return current;

        current.AllowedGarblerChannels = apiPerms.AllowedGarblerChannels;
        current.ChatGarblerActive = apiPerms.ChatGarblerActive;
        current.ChatGarblerLocked = apiPerms.ChatGarblerLocked;
        current.GaggedNameplate = apiPerms.GaggedNameplate;

        current.WardrobeEnabled = apiPerms.WardrobeEnabled;
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
        current.HypnosisCustomEffect = apiPerms.HypnosisCustomEffect;

        current.GlobalShockShareCode = apiPerms.GlobalShockShareCode;
        current.AllowShocks = apiPerms.AllowShocks;
        current.AllowVibrations = apiPerms.AllowVibrations;
        current.AllowBeeps = apiPerms.AllowBeeps;
        current.MaxIntensity = apiPerms.MaxIntensity;
        current.MaxDuration = apiPerms.MaxDuration;
        current.ShockVibrateDuration = apiPerms.ShockVibrateDuration;

        return current;
    }

    public static PairPerms ToApiKinksterPerms(this ClientPairPermissions? databasePerms)
    {
        var apiPerms = new PairPerms();
        if (databasePerms is null)
            return apiPerms;

        // Otherwise update it.
        apiPerms.IsPaused = databasePerms.IsPaused;

        apiPerms.PermanentLocks = databasePerms.PermanentLocks;
        apiPerms.OwnerLocks = databasePerms.OwnerLocks;
        apiPerms.DevotionalLocks = databasePerms.DevotionalLocks;

        apiPerms.ApplyGags = databasePerms.ApplyGags;
        apiPerms.LockGags = databasePerms.LockGags;
        apiPerms.MaxGagTime = databasePerms.MaxGagTime;
        apiPerms.UnlockGags = databasePerms.UnlockGags;
        apiPerms.RemoveGags = databasePerms.RemoveGags;

        apiPerms.ApplyRestrictions = databasePerms.ApplyRestrictions;
        apiPerms.LockRestrictions = databasePerms.LockRestrictions;
        apiPerms.MaxRestrictionTime = databasePerms.MaxRestrictionTime;
        apiPerms.UnlockRestrictions = databasePerms.UnlockRestrictions;
        apiPerms.RemoveRestrictions = databasePerms.RemoveRestrictions;

        apiPerms.ApplyRestraintSets = databasePerms.ApplyRestraintSets;
        apiPerms.ApplyLayers = databasePerms.ApplyLayers;
        apiPerms.ApplyLayersWhileLocked = databasePerms.ApplyLayersWhileLocked;
        apiPerms.LockRestraintSets = databasePerms.LockRestraintSets;
        apiPerms.MaxRestraintTime = databasePerms.MaxRestraintTime;
        apiPerms.UnlockRestraintSets = databasePerms.UnlockRestraintSets;
        apiPerms.RemoveLayers = databasePerms.RemoveLayers;
        apiPerms.RemoveLayersWhileLocked = databasePerms.RemoveLayersWhileLocked;
        apiPerms.RemoveRestraintSets = databasePerms.RemoveRestraintSets;

        apiPerms.TriggerPhrase = databasePerms.TriggerPhrase;
        apiPerms.StartChar = databasePerms.StartChar;
        apiPerms.EndChar = databasePerms.EndChar;
        apiPerms.PuppetPerms = databasePerms.PuppetPerms;

        apiPerms.MoodlePerms = databasePerms.MoodlePerms;
        apiPerms.MaxMoodleTime = databasePerms.MaxMoodleTime;

        apiPerms.ToggleToyState = databasePerms.ToggleToyState;
        apiPerms.RemoteControlAccess = databasePerms.RemoteControlAccess;
        apiPerms.ExecutePatterns = databasePerms.ExecutePatterns;
        apiPerms.StopPatterns = databasePerms.StopPatterns;
        apiPerms.ToggleAlarms = databasePerms.ToggleAlarms;
        apiPerms.ToggleTriggers = databasePerms.ToggleTriggers;

        apiPerms.HypnoEffectSending = databasePerms.HypnoEffectSending;

        apiPerms.InHardcore = databasePerms.InHardcore;
        apiPerms.PairLockedStates = databasePerms.PairLockedStates;
        apiPerms.AllowForcedFollow = databasePerms.AllowForcedFollow;
        apiPerms.AllowForcedSit = databasePerms.AllowForcedSit;
        apiPerms.AllowForcedEmote = databasePerms.AllowForcedEmote;
        apiPerms.AllowForcedStay = databasePerms.AllowForcedStay;
        apiPerms.AllowGarbleChannelEditing = databasePerms.AllowGarbleChannelEditing;
        apiPerms.AllowHidingChatBoxes = databasePerms.AllowHidingChatBoxes;
        apiPerms.AllowHidingChatInput = databasePerms.AllowHidingChatInput;
        apiPerms.AllowChatInputBlocking = databasePerms.AllowChatInputBlocking;
        apiPerms.AllowHypnoImageSending = databasePerms.AllowHypnoImageSending;

        apiPerms.PiShockShareCode = databasePerms.PiShockShareCode;
        apiPerms.AllowShocks = databasePerms.AllowShocks;
        apiPerms.AllowVibrations = databasePerms.AllowVibrations;
        apiPerms.AllowBeeps = databasePerms.AllowBeeps;
        apiPerms.MaxIntensity = databasePerms.MaxIntensity;
        apiPerms.MaxDuration = databasePerms.MaxDuration;
        apiPerms.MaxVibrateDuration = databasePerms.MaxVibrateDuration;

        return apiPerms;
    }

    public static ClientPairPermissions ToModelKinksterPerms(this PairPerms apiPerms, ClientPairPermissions databasePerms)
    {
        if (apiPerms is null)
            return databasePerms;

        databasePerms.IsPaused = apiPerms.IsPaused;

        databasePerms.PermanentLocks = apiPerms.PermanentLocks;
        databasePerms.OwnerLocks = apiPerms.OwnerLocks;
        databasePerms.DevotionalLocks = apiPerms.DevotionalLocks;

        databasePerms.ApplyGags = apiPerms.ApplyGags;
        databasePerms.LockGags = apiPerms.LockGags;
        databasePerms.MaxGagTime = apiPerms.MaxGagTime;
        databasePerms.UnlockGags = apiPerms.UnlockGags;
        databasePerms.RemoveGags = apiPerms.RemoveGags;

        databasePerms.ApplyRestrictions = apiPerms.ApplyRestrictions;
        databasePerms.LockRestrictions = apiPerms.LockRestrictions;
        databasePerms.MaxRestrictionTime = apiPerms.MaxRestrictionTime;
        databasePerms.UnlockRestrictions = apiPerms.UnlockRestrictions;
        databasePerms.RemoveRestrictions = apiPerms.RemoveRestrictions;

        databasePerms.ApplyRestraintSets = apiPerms.ApplyRestraintSets;
        databasePerms.ApplyLayers = apiPerms.ApplyLayers;
        databasePerms.ApplyLayersWhileLocked = apiPerms.ApplyLayersWhileLocked;
        databasePerms.LockRestraintSets = apiPerms.LockRestraintSets;
        databasePerms.MaxRestraintTime = apiPerms.MaxRestraintTime;
        databasePerms.UnlockRestraintSets = apiPerms.UnlockRestraintSets;
        databasePerms.RemoveLayers = apiPerms.RemoveLayers;
        databasePerms.RemoveLayersWhileLocked = apiPerms.RemoveLayersWhileLocked;
        databasePerms.RemoveRestraintSets = apiPerms.RemoveRestraintSets;

        databasePerms.TriggerPhrase = apiPerms.TriggerPhrase;
        databasePerms.StartChar = apiPerms.StartChar;
        databasePerms.EndChar = apiPerms.EndChar;
        databasePerms.PuppetPerms = apiPerms.PuppetPerms;

        databasePerms.MoodlePerms = apiPerms.MoodlePerms;
        databasePerms.MaxMoodleTime = apiPerms.MaxMoodleTime;

        databasePerms.ToggleToyState = apiPerms.ToggleToyState;
        databasePerms.RemoteControlAccess = apiPerms.RemoteControlAccess;
        databasePerms.ExecutePatterns = apiPerms.ExecutePatterns;
        databasePerms.StopPatterns = apiPerms.StopPatterns;
        databasePerms.ToggleAlarms = apiPerms.ToggleAlarms;
        databasePerms.ToggleTriggers = apiPerms.ToggleTriggers;

        databasePerms.HypnoEffectSending = apiPerms.HypnoEffectSending;

        databasePerms.InHardcore = apiPerms.InHardcore;
        databasePerms.PairLockedStates = apiPerms.PairLockedStates;
        databasePerms.AllowForcedFollow = apiPerms.AllowForcedFollow;
        databasePerms.AllowForcedSit = apiPerms.AllowForcedSit;
        databasePerms.AllowForcedEmote = apiPerms.AllowForcedEmote;
        databasePerms.AllowForcedStay = apiPerms.AllowForcedStay;
        databasePerms.AllowGarbleChannelEditing = apiPerms.AllowGarbleChannelEditing;
        databasePerms.AllowHidingChatBoxes = apiPerms.AllowHidingChatBoxes;
        databasePerms.AllowHidingChatInput = apiPerms.AllowHidingChatInput;
        databasePerms.AllowChatInputBlocking = apiPerms.AllowChatInputBlocking;
        databasePerms.AllowHypnoImageSending = apiPerms.AllowHypnoImageSending;

        databasePerms.PiShockShareCode = apiPerms.PiShockShareCode;
        databasePerms.AllowShocks = apiPerms.AllowShocks;
        databasePerms.AllowVibrations = apiPerms.AllowVibrations;
        databasePerms.AllowBeeps = apiPerms.AllowBeeps;
        databasePerms.MaxIntensity = apiPerms.MaxIntensity;
        databasePerms.MaxDuration = apiPerms.MaxDuration;
        databasePerms.MaxVibrateDuration = apiPerms.MaxVibrateDuration;

        return databasePerms;
    }

    public static PairPermAccess ToApiKinksterEditAccess(this ClientPairPermissionAccess? databasePerms)
    {
        var apiPerms = new PairPermAccess();
        if (databasePerms is null)
            return apiPerms;

        apiPerms.ChatGarblerActiveAllowed = databasePerms.ChatGarblerActiveAllowed;
        apiPerms.ChatGarblerLockedAllowed = databasePerms.ChatGarblerLockedAllowed;

        apiPerms.WardrobeEnabledAllowed = databasePerms.WardrobeEnabledAllowed;
        apiPerms.GagVisualsAllowed = databasePerms.GagVisualsAllowed;
        apiPerms.RestrictionVisualsAllowed = databasePerms.RestrictionVisualsAllowed;
        apiPerms.RestraintSetVisualsAllowed = databasePerms.RestraintSetVisualsAllowed;

        apiPerms.PermanentLocksAllowed = databasePerms.PermanentLocksAllowed;
        apiPerms.OwnerLocksAllowed = databasePerms.OwnerLocksAllowed;
        apiPerms.DevotionalLocksAllowed = databasePerms.DevotionalLocksAllowed;

        apiPerms.ApplyGagsAllowed = databasePerms.ApplyGagsAllowed;
        apiPerms.LockGagsAllowed = databasePerms.LockGagsAllowed;
        apiPerms.MaxGagTimeAllowed = databasePerms.MaxGagTimeAllowed;
        apiPerms.UnlockGagsAllowed = databasePerms.UnlockGagsAllowed;
        apiPerms.RemoveGagsAllowed = databasePerms.RemoveGagsAllowed;

        apiPerms.ApplyRestrictionsAllowed = databasePerms.ApplyRestrictionsAllowed;
        apiPerms.LockRestrictionsAllowed = databasePerms.LockRestrictionsAllowed;
        apiPerms.MaxRestrictionTimeAllowed = databasePerms.MaxRestrictionTimeAllowed;
        apiPerms.UnlockRestrictionsAllowed = databasePerms.UnlockRestrictionsAllowed;
        apiPerms.RemoveRestrictionsAllowed = databasePerms.RemoveRestrictionsAllowed;

        apiPerms.ApplyRestraintSetsAllowed = databasePerms.ApplyRestraintSetsAllowed;
        apiPerms.ApplyLayersAllowed = databasePerms.ApplyLayersAllowed;
        apiPerms.ApplyLayersWhileLockedAllowed = databasePerms.ApplyLayersWhileLockedAllowed;
        apiPerms.LockRestraintSetsAllowed = databasePerms.LockRestraintSetsAllowed;
        apiPerms.MaxRestraintTimeAllowed = databasePerms.MaxRestraintTimeAllowed;
        apiPerms.UnlockRestraintSetsAllowed = databasePerms.UnlockRestraintSetsAllowed;
        apiPerms.RemoveLayersAllowed = databasePerms.RemoveLayersAllowed;
        apiPerms.RemoveLayersWhileLockedAllowed = databasePerms.RemoveLayersWhileLockedAllowed;
        apiPerms.RemoveRestraintSetsAllowed = databasePerms.RemoveRestraintSetsAllowed;

        apiPerms.PuppeteerEnabledAllowed = databasePerms.PuppeteerEnabledAllowed;
        apiPerms.PuppetPermsAllowed = databasePerms.PuppetPermsAllowed;

        apiPerms.MoodlesEnabledAllowed = databasePerms.MoodlesEnabledAllowed;
        apiPerms.MoodlePermsAllowed = databasePerms.MoodlePermsAllowed;
        apiPerms.MaxMoodleTimeAllowed = databasePerms.MaxMoodleTimeAllowed;

        apiPerms.ToyboxEnabledAllowed = databasePerms.ToyboxEnabledAllowed;
        apiPerms.LockToyboxUIAllowed = databasePerms.LockToyboxUIAllowed;
        apiPerms.SpatialAudioAllowed = databasePerms.SpatialAudioAllowed;
        apiPerms.ToggleToyStateAllowed = databasePerms.ToggleToyStateAllowed;
        apiPerms.RemoteControlAccessAllowed = databasePerms.RemoteControlAccessAllowed;
        apiPerms.ExecutePatternsAllowed = databasePerms.ExecutePatternsAllowed;
        apiPerms.StopPatternsAllowed = databasePerms.StopPatternsAllowed;
        apiPerms.ToggleAlarmsAllowed = databasePerms.ToggleAlarmsAllowed;
        apiPerms.ToggleTriggersAllowed = databasePerms.ToggleTriggersAllowed;

        apiPerms.HypnoEffectSendingAllowed = databasePerms.HypnoEffectSendingAllowed;

        return apiPerms;
    }

    public static ClientPairPermissionAccess ToModelKinksterEditAccess(this PairPermAccess apiPerms, ClientPairPermissionAccess databasePerms)
    {
        if (apiPerms is null)
            return databasePerms;

        // Otherwise update it.
        databasePerms.ChatGarblerActiveAllowed = apiPerms.ChatGarblerActiveAllowed;
        databasePerms.ChatGarblerLockedAllowed = apiPerms.ChatGarblerLockedAllowed;

        databasePerms.WardrobeEnabledAllowed = apiPerms.WardrobeEnabledAllowed;
        databasePerms.GagVisualsAllowed = apiPerms.GagVisualsAllowed;
        databasePerms.RestrictionVisualsAllowed = apiPerms.RestrictionVisualsAllowed;
        databasePerms.RestraintSetVisualsAllowed = apiPerms.RestraintSetVisualsAllowed;

        databasePerms.PermanentLocksAllowed = apiPerms.PermanentLocksAllowed;
        databasePerms.OwnerLocksAllowed = apiPerms.OwnerLocksAllowed;
        databasePerms.DevotionalLocksAllowed = apiPerms.DevotionalLocksAllowed;

        databasePerms.ApplyGagsAllowed = apiPerms.ApplyGagsAllowed;
        databasePerms.LockGagsAllowed = apiPerms.LockGagsAllowed;
        databasePerms.MaxGagTimeAllowed = apiPerms.MaxGagTimeAllowed;
        databasePerms.UnlockGagsAllowed = apiPerms.UnlockGagsAllowed;
        databasePerms.RemoveGagsAllowed = apiPerms.RemoveGagsAllowed;

        databasePerms.ApplyRestrictionsAllowed = apiPerms.ApplyRestrictionsAllowed;
        databasePerms.LockRestrictionsAllowed = apiPerms.LockRestrictionsAllowed;
        databasePerms.MaxRestrictionTimeAllowed = apiPerms.MaxRestrictionTimeAllowed;
        databasePerms.UnlockRestrictionsAllowed = apiPerms.UnlockRestrictionsAllowed;
        databasePerms.RemoveRestrictionsAllowed = apiPerms.RemoveRestrictionsAllowed;

        databasePerms.ApplyRestraintSetsAllowed = apiPerms.ApplyRestraintSetsAllowed;
        databasePerms.ApplyLayersAllowed = apiPerms.ApplyLayersAllowed;
        databasePerms.ApplyLayersWhileLockedAllowed = apiPerms.ApplyLayersWhileLockedAllowed;
        databasePerms.LockRestraintSetsAllowed = apiPerms.LockRestraintSetsAllowed;
        databasePerms.MaxRestraintTimeAllowed = apiPerms.MaxRestraintTimeAllowed;
        databasePerms.UnlockRestraintSetsAllowed = apiPerms.UnlockRestraintSetsAllowed;
        databasePerms.RemoveLayersAllowed = apiPerms.RemoveLayersAllowed;
        databasePerms.RemoveLayersWhileLockedAllowed = apiPerms.RemoveLayersWhileLockedAllowed;
        databasePerms.RemoveRestraintSetsAllowed = apiPerms.RemoveRestraintSetsAllowed;

        databasePerms.PuppeteerEnabledAllowed = apiPerms.PuppeteerEnabledAllowed;
        databasePerms.PuppetPermsAllowed = apiPerms.PuppetPermsAllowed;

        databasePerms.MoodlesEnabledAllowed = apiPerms.MoodlesEnabledAllowed;
        databasePerms.MoodlePermsAllowed = apiPerms.MoodlePermsAllowed;
        databasePerms.MaxMoodleTimeAllowed = apiPerms.MaxMoodleTimeAllowed;

        databasePerms.ToyboxEnabledAllowed = apiPerms.ToyboxEnabledAllowed;
        databasePerms.LockToyboxUIAllowed = apiPerms.LockToyboxUIAllowed;
        databasePerms.SpatialAudioAllowed = apiPerms.SpatialAudioAllowed;
        databasePerms.ToggleToyStateAllowed = apiPerms.ToggleToyStateAllowed;
        databasePerms.RemoteControlAccessAllowed = apiPerms.RemoteControlAccessAllowed;
        databasePerms.ExecutePatternsAllowed = apiPerms.ExecutePatternsAllowed;
        databasePerms.StopPatternsAllowed = apiPerms.StopPatternsAllowed;
        databasePerms.ToggleAlarmsAllowed = apiPerms.ToggleAlarmsAllowed;
        databasePerms.ToggleTriggersAllowed = apiPerms.ToggleTriggersAllowed;
        
        databasePerms.HypnoEffectSendingAllowed = apiPerms.HypnoEffectSendingAllowed;

        return databasePerms;
    }
}
#pragma warning restore MA0051
#nullable disable