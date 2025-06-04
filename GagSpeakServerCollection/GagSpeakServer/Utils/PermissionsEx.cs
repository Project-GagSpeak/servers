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
            LayersBitfield = restraintSet.LayersBitfield,
            Enabler = restraintSet.Enabler,
            Padlock = restraintSet.Padlock,
            Password = restraintSet.Password,
            Timer = restraintSet.Timer,
            PadlockAssigner = restraintSet.PadlockAssigner
        };
    }
    #endregion CacheDataMigrations

    public static GlobalPerms ToApiGlobalPerms(this UserGlobalPermissions? globalPermsModel)
    {
        var result = new GlobalPerms();

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

    public static UserGlobalPermissions ToModelGlobalPerms(this GlobalPerms apiPerms, UserGlobalPermissions current)
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

    public static PairPerms ToApiUserPairPerms(this ClientPairPermissions? clientPairPermsModel)
    {
        var result = new PairPerms();
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

    public static ClientPairPermissions ToModelUserPairPerms(this PairPerms apiPairPerms, ClientPairPermissions current)
    {
        if (apiPairPerms is null)
            return current;

        current.IsPaused = apiPairPerms.IsPaused;

        current.PermanentLocks = apiPairPerms.PermanentLocks;
        current.OwnerLocks = apiPairPerms.OwnerLocks;
        current.DevotionalLocks = apiPairPerms.DevotionalLocks;

        current.ApplyGags = apiPairPerms.ApplyGags;
        current.LockGags = apiPairPerms.LockGags;
        current.MaxGagTime = apiPairPerms.MaxGagTime;
        current.UnlockGags = apiPairPerms.UnlockGags;
        current.RemoveGags = apiPairPerms.RemoveGags;

        current.ApplyRestrictions = apiPairPerms.ApplyRestrictions;
        current.LockRestrictions = apiPairPerms.LockRestrictions;
        current.MaxRestrictionTime = apiPairPerms.MaxRestrictionTime;
        current.UnlockRestrictions = apiPairPerms.UnlockRestrictions;
        current.RemoveRestrictions = apiPairPerms.RemoveRestrictions;

        current.ApplyRestraintSets = apiPairPerms.ApplyRestraintSets;
        current.ApplyRestraintLayers = apiPairPerms.ApplyRestraintLayers;
        current.LockRestraintSets = apiPairPerms.LockRestraintSets;
        current.MaxRestraintTime = apiPairPerms.MaxRestraintTime;
        current.UnlockRestraintSets = apiPairPerms.UnlockRestraintSets;
        current.RemoveRestraintSets = apiPairPerms.RemoveRestraintSets;

        current.TriggerPhrase = apiPairPerms.TriggerPhrase;
        current.StartChar = apiPairPerms.StartChar;
        current.EndChar = apiPairPerms.EndChar;
        current.PuppetPerms = apiPairPerms.PuppetPerms;

        current.MoodlePerms = apiPairPerms.MoodlePerms;
        current.MaxMoodleTime = apiPairPerms.MaxMoodleTime;

        current.ToggleToyState = apiPairPerms.ToggleToyState;
        current.RemoteControlAccess = apiPairPerms.RemoteControlAccess;
        current.ExecutePatterns = apiPairPerms.ExecutePatterns;
        current.StopPatterns = apiPairPerms.StopPatterns;
        current.ToggleAlarms = apiPairPerms.ToggleAlarms;
        current.ToggleTriggers = apiPairPerms.ToggleTriggers;

        current.InHardcore = apiPairPerms.InHardcore;
        current.PairLockedStates = apiPairPerms.PairLockedStates;
        current.AllowForcedFollow = apiPairPerms.AllowForcedFollow;
        current.AllowForcedSit = apiPairPerms.AllowForcedSit;
        current.AllowForcedEmote = apiPairPerms.AllowForcedEmote;
        current.AllowForcedStay = apiPairPerms.AllowForcedStay;
        current.AllowGarbleChannelEditing = apiPairPerms.AllowGarbleChannelEditing;
        current.AllowHidingChatBoxes = apiPairPerms.AllowHidingChatBoxes;
        current.AllowHidingChatInput = apiPairPerms.AllowHidingChatInput;
        current.AllowChatInputBlocking = apiPairPerms.AllowChatInputBlocking;

        current.PiShockShareCode = apiPairPerms.PiShockShareCode;
        current.AllowShocks = apiPairPerms.AllowShocks;
        current.AllowVibrations = apiPairPerms.AllowVibrations;
        current.AllowBeeps = apiPairPerms.AllowBeeps;
        current.MaxIntensity = apiPairPerms.MaxIntensity;
        current.MaxDuration = apiPairPerms.MaxDuration;
        current.MaxVibrateDuration = apiPairPerms.MaxVibrateDuration;

        return current;
    }

    public static PairPermAccess ToApiUserPairEditAccessPerms(this ClientPairPermissionAccess? pairAccessPermsModel)
    {
        var result = new PairPermAccess();
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

    public static ClientPairPermissionAccess ToModelUserPairEditAccessPerms(this PairPermAccess api, ClientPairPermissionAccess current)
    {
        if (api is null) return current;

        // Otherwise update it.
        current.ChatGarblerActiveAllowed = api.ChatGarblerActiveAllowed;
        current.ChatGarblerLockedAllowed = api.ChatGarblerLockedAllowed;

        current.WardrobeEnabledAllowed = api.WardrobeEnabledAllowed;
        current.GagVisualsAllowed = api.GagVisualsAllowed;
        current.RestrictionVisualsAllowed = api.RestrictionVisualsAllowed;
        current.RestraintSetVisualsAllowed = api.RestraintSetVisualsAllowed;

        current.PermanentLocksAllowed = api.PermanentLocksAllowed;
        current.OwnerLocksAllowed = api.OwnerLocksAllowed;
        current.DevotionalLocksAllowed = api.DevotionalLocksAllowed;

        current.ApplyGagsAllowed = api.ApplyGagsAllowed;
        current.LockGagsAllowed = api.LockGagsAllowed;
        current.MaxGagTimeAllowed = api.MaxGagTimeAllowed;
        current.UnlockGagsAllowed = api.UnlockGagsAllowed;
        current.RemoveGagsAllowed = api.RemoveGagsAllowed;

        current.ApplyRestrictionsAllowed = api.ApplyRestrictionsAllowed;
        current.LockRestrictionsAllowed = api.LockRestrictionsAllowed;
        current.MaxRestrictionTimeAllowed = api.MaxRestrictionTimeAllowed;
        current.UnlockRestrictionsAllowed = api.UnlockRestrictionsAllowed;
        current.RemoveRestrictionsAllowed = api.RemoveRestrictionsAllowed;

        current.ApplyRestraintSetsAllowed = api.ApplyRestraintSetsAllowed;
        current.ApplyRestraintLayersAllowed = api.ApplyRestraintLayersAllowed;
        current.LockRestraintSetsAllowed = api.LockRestraintSetsAllowed;
        current.MaxRestrictionTimeAllowed = api.MaxRestrictionTimeAllowed;
        current.UnlockRestraintSetsAllowed = api.UnlockRestraintSetsAllowed;
        current.RemoveRestraintSetsAllowed = api.RemoveRestraintSetsAllowed;

        current.PuppeteerEnabledAllowed = api.PuppeteerEnabledAllowed;
        current.PuppetPermsAllowed = api.PuppetPermsAllowed;

        current.MoodlesEnabledAllowed = api.MoodlesEnabledAllowed;
        current.MoodlePermsAllowed = api.MoodlePermsAllowed;
        current.MaxMoodleTimeAllowed = api.MaxMoodleTimeAllowed;

        current.ToyboxEnabledAllowed = api.ToyboxEnabledAllowed;
        current.LockToyboxUIAllowed = api.LockToyboxUIAllowed;
        current.SpatialAudioAllowed = api.SpatialAudioAllowed;
        current.ToggleToyStateAllowed = api.ToggleToyStateAllowed;
        current.RemoteControlAccessAllowed = api.RemoteControlAccessAllowed;
        current.ExecutePatternsAllowed = api.ExecutePatternsAllowed;
        current.StopPatternsAllowed = api.StopPatternsAllowed;
        current.ToggleAlarmsAllowed = api.ToggleAlarmsAllowed;
        current.ToggleTriggersAllowed = api.ToggleTriggersAllowed;

        return current;
    }
}
#pragma warning restore MA0051
#nullable disable