using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Network;
using GagspeakAPI.Util;
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

    public static CharaActiveCollar ToApiCollarData(this UserCollarData data)
    {
        return new CharaActiveCollar
        {
            OwnerUIDs = data.Owners.Select(o => o.OwnerUID).ToList(),
            Visuals = data.Visuals,
            Dye1 = data.Dye1,
            Dye2 = data.Dye2,
            Moodle = MoodleConverter.FromValues(data.MoodleId, data.MoodleIconId, data.MoodleTitle, data.MoodleDescription, data.MoodleType, data.MoodleVFXPath),
            Writing = data.Writing
        };
    }

    public static KinksterRequest ToApi(this PairRequest req)
        => new KinksterRequest(new(req.UserUID), new(req.OtherUserUID), new(false, req.PreferredNickname, req.AttachedMessage), req.CreationTime);

    public static KinksterRequest ToApiRemoval(UserData user, UserData target)
        => new KinksterRequest(user, target, new(false, string.Empty, string.Empty), DateTime.MinValue);

    public static CollarRequest ToApiCollarRequest(this CollaringRequest request)
        => new CollarRequest(new(request.UserUID), new(request.OtherUserUID), request.InitialWriting, request.CreationTime, request.OtherUserAccess, request.OwnerAccess);
    

    public static CollarRequest CollarRequestRemoval(UserData user, UserData target) =>
        new(user, target, string.Empty, DateTime.MinValue, CollarAccess.None, CollarAccess.None);

    public static UserReputation ToApi(this AccountReputation? dbState)
        => dbState is null ? new() : new UserReputation()
        {
            IsVerified = dbState.IsVerified,
            IsBanned = dbState.IsBanned,
            WarningStrikes = dbState.WarningStrikes,
            ProfileViewing = dbState.ProfileViewing,
            ProfileViewStrikes = dbState.ProfileViewStrikes,
            ProfileEditing = dbState.ProfileEditing,
            ProfileEditStrikes = dbState.ProfileEditStrikes,
            ChatUsage = dbState.ChatUsage,
            ChatStrikes = dbState.ChatStrikes
        };

    #endregion CacheDataMigrations

    public static GlobalPerms ToApi(this GlobalPermissions? dbState)
    {
        var apiPerms = new GlobalPerms();

        if (dbState is null)
            return apiPerms;

        apiPerms.AllowedGarblerChannels = dbState.AllowedGarblerChannels;
        apiPerms.ChatGarblerActive = dbState.ChatGarblerActive;
        apiPerms.ChatGarblerLocked = dbState.ChatGarblerLocked;
        apiPerms.GaggedNameplate = dbState.GaggedNameplate;

        apiPerms.WardrobeEnabled = dbState.WardrobeEnabled;
        apiPerms.GagVisuals = dbState.GagVisuals;
        apiPerms.RestrictionVisuals = dbState.RestrictionVisuals;
        apiPerms.RestraintSetVisuals = dbState.RestraintSetVisuals;

        apiPerms.PuppeteerEnabled = dbState.PuppeteerEnabled;
        apiPerms.TriggerPhrase = dbState.TriggerPhrase;
        apiPerms.PuppetPerms = dbState.PuppetPerms;

        apiPerms.ToyboxEnabled = dbState.ToyboxEnabled;
        apiPerms.ToysAreInteractable = dbState.ToysAreInteractable;
        apiPerms.InVibeRoom = dbState.InVibeRoom;
        apiPerms.SpatialAudio = dbState.SpatialAudio;

        apiPerms.GlobalShockShareCode = dbState.GlobalShockShareCode;
        apiPerms.AllowShocks = dbState.AllowShocks;
        apiPerms.AllowVibrations = dbState.AllowVibrations;
        apiPerms.AllowBeeps = dbState.AllowBeeps;
        apiPerms.MaxIntensity = dbState.MaxIntensity;
        apiPerms.MaxDuration = dbState.MaxDuration;
        apiPerms.ShockVibrateDuration = dbState.ShockVibrateDuration;

        return apiPerms;
    }

    public static GlobalPermissions ToModel(this GlobalPerms apiPerms, GlobalPermissions current)
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
        current.ToysAreInteractable = apiPerms.ToysAreInteractable;
        current.InVibeRoom = apiPerms.InVibeRoom;
        current.SpatialAudio = apiPerms.SpatialAudio;

        current.GlobalShockShareCode = apiPerms.GlobalShockShareCode;
        current.AllowShocks = apiPerms.AllowShocks;
        current.AllowVibrations = apiPerms.AllowVibrations;
        current.AllowBeeps = apiPerms.AllowBeeps;
        current.MaxIntensity = apiPerms.MaxIntensity;
        current.MaxDuration = apiPerms.MaxDuration;
        current.ShockVibrateDuration = apiPerms.ShockVibrateDuration;

        return current;
    }

    public static HardcoreStatus ToApi(this HardcoreState? dbState)
    {
        var apiState = new HardcoreStatus();
        if (dbState is null)
            return apiState;

        // Otherwise update it.
        apiState.LockedFollowing = dbState.LockedFollowing;

        apiState.LockedEmoteState = dbState.LockedEmoteState;
        apiState.EmoteExpireTime = dbState.EmoteExpireTime;
        apiState.EmoteId = dbState.EmoteId;
        apiState.EmoteCyclePose = dbState.EmoteCyclePose;

        apiState.IndoorConfinement = dbState.IndoorConfinement;
        apiState.ConfinementTimer = dbState.ConfinementTimer;
        apiState.ConfinedWorld = dbState.ConfinedWorld;
        apiState.ConfinedCity = dbState.ConfinedCity;
        apiState.ConfinedWard = dbState.ConfinedWard;
        apiState.ConfinedPlaceId = dbState.ConfinedPlaceId;
        apiState.ConfinedInApartment = dbState.ConfinedInApartment;
        apiState.ConfinedInSubdivision = dbState.ConfinedInSubdivision;

        apiState.Imprisonment = dbState.Imprisonment;
        apiState.ImprisonmentTimer = dbState.ImprisonmentTimer;
        apiState.ImprisonedTerritory = dbState.ImprisonedTerritory;
        apiState.ImprisonedPos = new(dbState.ImprisonedPosX, dbState.ImprisonedPosY, dbState.ImprisonedPosZ);
        apiState.ImprisonedRadius = dbState.ImprisonedRadius;

        apiState.ChatBoxesHidden = dbState.ChatBoxesHidden;
        apiState.ChatBoxesHiddenTimer = dbState.ChatBoxesHiddenTimer;

        apiState.ChatInputHidden = dbState.ChatInputHidden;
        apiState.ChatInputHiddenTimer = dbState.ChatInputHiddenTimer;

        apiState.ChatInputBlocked = dbState.ChatInputBlocked;
        apiState.ChatInputBlockedTimer = dbState.ChatInputBlockedTimer;

        apiState.HypnoticEffect = dbState.HypnoticEffect;
        apiState.HypnoticEffectTimer = dbState.HypnoticEffectTimer;
        return apiState;
    }

    public static HardcoreState ToModel(this HardcoreStatus apiState, HardcoreState current)
    {
        if (apiState is null)
            return current;

        current.LockedFollowing = apiState.LockedFollowing;

        current.LockedEmoteState = apiState.LockedEmoteState;
        current.EmoteExpireTime = apiState.EmoteExpireTime;
        current.EmoteId = apiState.EmoteId;
        current.EmoteCyclePose = apiState.EmoteCyclePose;

        current.IndoorConfinement = apiState.IndoorConfinement;
        current.ConfinementTimer = apiState.ConfinementTimer;
        current.ConfinedWorld = apiState.ConfinedWorld;
        current.ConfinedCity = apiState.ConfinedCity;
        current.ConfinedWard = apiState.ConfinedWard;
        current.ConfinedPlaceId = apiState.ConfinedPlaceId;
        current.ConfinedInApartment = apiState.ConfinedInApartment;
        current.ConfinedInSubdivision = apiState.ConfinedInSubdivision;

        current.Imprisonment = apiState.Imprisonment;
        current.ImprisonmentTimer = apiState.ImprisonmentTimer;
        current.ImprisonedTerritory = apiState.ImprisonedTerritory;
        current.ImprisonedPosX = apiState.ImprisonedPos.X;
        current.ImprisonedPosY = apiState.ImprisonedPos.Y;
        current.ImprisonedPosZ = apiState.ImprisonedPos.Z;
        current.ImprisonedRadius = apiState.ImprisonedRadius;

        current.ChatBoxesHidden = apiState.ChatBoxesHidden;
        current.ChatBoxesHiddenTimer = apiState.ChatBoxesHiddenTimer;

        current.ChatInputHidden = apiState.ChatInputHidden;
        current.ChatInputHiddenTimer = apiState.ChatInputHiddenTimer;

        current.ChatInputBlocked = apiState.ChatInputBlocked;
        current.ChatInputBlockedTimer = apiState.ChatInputBlockedTimer;

        current.HypnoticEffect = apiState.HypnoticEffect;
        current.HypnoticEffectTimer = apiState.HypnoticEffectTimer;
        return current;
    }

    public static PairPerms ToApi(this PairPermissions? dbState)
    {
        var apiPerms = new PairPerms();
        if (dbState is null)
            return apiPerms;

        apiPerms.PermanentLocks = dbState.PermanentLocks;
        apiPerms.OwnerLocks = dbState.OwnerLocks;
        apiPerms.DevotionalLocks = dbState.DevotionalLocks;

        apiPerms.ApplyGags = dbState.ApplyGags;
        apiPerms.LockGags = dbState.LockGags;
        apiPerms.MaxGagTime = dbState.MaxGagTime;
        apiPerms.UnlockGags = dbState.UnlockGags;
        apiPerms.RemoveGags = dbState.RemoveGags;

        apiPerms.ApplyRestrictions = dbState.ApplyRestrictions;
        apiPerms.LockRestrictions = dbState.LockRestrictions;
        apiPerms.MaxRestrictionTime = dbState.MaxRestrictionTime;
        apiPerms.UnlockRestrictions = dbState.UnlockRestrictions;
        apiPerms.RemoveRestrictions = dbState.RemoveRestrictions;

        apiPerms.ApplyRestraintSets = dbState.ApplyRestraintSets;
        apiPerms.ApplyLayers = dbState.ApplyLayers;
        apiPerms.ApplyLayersWhileLocked = dbState.ApplyLayersWhileLocked;
        apiPerms.LockRestraintSets = dbState.LockRestraintSets;
        apiPerms.MaxRestraintTime = dbState.MaxRestraintTime;
        apiPerms.UnlockRestraintSets = dbState.UnlockRestraintSets;
        apiPerms.RemoveLayers = dbState.RemoveLayers;
        apiPerms.RemoveLayersWhileLocked = dbState.RemoveLayersWhileLocked;
        apiPerms.RemoveRestraintSets = dbState.RemoveRestraintSets;

        apiPerms.TriggerPhrase = dbState.TriggerPhrase;
        apiPerms.StartChar = dbState.StartChar;
        apiPerms.EndChar = dbState.EndChar;
        apiPerms.PuppetPerms = dbState.PuppetPerms;

        apiPerms.MoodleAccess = dbState.MoodleAccess;
        apiPerms.MaxMoodleTime = dbState.MaxMoodleTime;

        apiPerms.MaxHypnosisTime = dbState.MaxHypnosisTime;
        apiPerms.HypnoEffectSending = dbState.HypnoEffectSending;

        apiPerms.ExecutePatterns = dbState.ExecutePatterns;
        apiPerms.StopPatterns = dbState.StopPatterns;
        apiPerms.ToggleAlarms = dbState.ToggleAlarms;
        apiPerms.ToggleTriggers = dbState.ToggleTriggers;

        apiPerms.InHardcore = dbState.InHardcore;
        apiPerms.PairLockedStates = dbState.PairLockedStates;
        apiPerms.AllowLockedFollowing = dbState.AllowLockedFollowing;
        apiPerms.AllowLockedSitting = dbState.AllowLockedSitting;
        apiPerms.AllowLockedEmoting = dbState.AllowLockedEmoting;
        apiPerms.AllowIndoorConfinement = dbState.AllowIndoorConfinement;
        apiPerms.AllowImprisonment = dbState.AllowImprisonment;
        apiPerms.AllowGarbleChannelEditing = dbState.AllowGarbleChannelEditing;
        apiPerms.AllowHidingChatBoxes = dbState.AllowHidingChatBoxes;
        apiPerms.AllowHidingChatInput = dbState.AllowHidingChatInput;
        apiPerms.AllowChatInputBlocking = dbState.AllowChatInputBlocking;
        apiPerms.AllowHypnoImageSending = dbState.AllowHypnoImageSending;

        apiPerms.PiShockShareCode = dbState.PiShockShareCode;
        apiPerms.AllowShocks = dbState.AllowShocks;
        apiPerms.AllowVibrations = dbState.AllowVibrations;
        apiPerms.AllowBeeps = dbState.AllowBeeps;
        apiPerms.MaxIntensity = dbState.MaxIntensity;
        apiPerms.MaxDuration = dbState.MaxDuration;
        apiPerms.MaxVibrateDuration = dbState.MaxVibrateDuration;

        return apiPerms;
    }

    public static PairPermissions ToModel(this PairPerms apiPerms, PairPermissions dbState)
    {
        if (apiPerms is null)
            return dbState;

        dbState.PermanentLocks = apiPerms.PermanentLocks;
        dbState.OwnerLocks = apiPerms.OwnerLocks;
        dbState.DevotionalLocks = apiPerms.DevotionalLocks;

        dbState.ApplyGags = apiPerms.ApplyGags;
        dbState.LockGags = apiPerms.LockGags;
        dbState.MaxGagTime = apiPerms.MaxGagTime;
        dbState.UnlockGags = apiPerms.UnlockGags;
        dbState.RemoveGags = apiPerms.RemoveGags;

        dbState.ApplyRestrictions = apiPerms.ApplyRestrictions;
        dbState.LockRestrictions = apiPerms.LockRestrictions;
        dbState.MaxRestrictionTime = apiPerms.MaxRestrictionTime;
        dbState.UnlockRestrictions = apiPerms.UnlockRestrictions;
        dbState.RemoveRestrictions = apiPerms.RemoveRestrictions;

        dbState.ApplyRestraintSets = apiPerms.ApplyRestraintSets;
        dbState.ApplyLayers = apiPerms.ApplyLayers;
        dbState.ApplyLayersWhileLocked = apiPerms.ApplyLayersWhileLocked;
        dbState.LockRestraintSets = apiPerms.LockRestraintSets;
        dbState.MaxRestraintTime = apiPerms.MaxRestraintTime;
        dbState.UnlockRestraintSets = apiPerms.UnlockRestraintSets;
        dbState.RemoveLayers = apiPerms.RemoveLayers;
        dbState.RemoveLayersWhileLocked = apiPerms.RemoveLayersWhileLocked;
        dbState.RemoveRestraintSets = apiPerms.RemoveRestraintSets;

        dbState.TriggerPhrase = apiPerms.TriggerPhrase;
        dbState.StartChar = apiPerms.StartChar;
        dbState.EndChar = apiPerms.EndChar;
        dbState.PuppetPerms = apiPerms.PuppetPerms;

        dbState.MoodleAccess = apiPerms.MoodleAccess;
        dbState.MaxMoodleTime = apiPerms.MaxMoodleTime;

        dbState.MaxHypnosisTime = apiPerms.MaxHypnosisTime;
        dbState.HypnoEffectSending = apiPerms.HypnoEffectSending;

        dbState.ExecutePatterns = apiPerms.ExecutePatterns;
        dbState.StopPatterns = apiPerms.StopPatterns;
        dbState.ToggleAlarms = apiPerms.ToggleAlarms;
        dbState.ToggleTriggers = apiPerms.ToggleTriggers;

        dbState.InHardcore = apiPerms.InHardcore;
        dbState.PairLockedStates = apiPerms.PairLockedStates;
        dbState.AllowLockedFollowing = apiPerms.AllowLockedFollowing;
        dbState.AllowLockedSitting = apiPerms.AllowLockedSitting;
        dbState.AllowLockedEmoting = apiPerms.AllowLockedEmoting;
        dbState.AllowIndoorConfinement = apiPerms.AllowIndoorConfinement;
        dbState.AllowImprisonment = apiPerms.AllowImprisonment;
        dbState.AllowGarbleChannelEditing = apiPerms.AllowGarbleChannelEditing;
        dbState.AllowHidingChatBoxes = apiPerms.AllowHidingChatBoxes;
        dbState.AllowHidingChatInput = apiPerms.AllowHidingChatInput;
        dbState.AllowChatInputBlocking = apiPerms.AllowChatInputBlocking;
        dbState.AllowHypnoImageSending = apiPerms.AllowHypnoImageSending;

        dbState.PiShockShareCode = apiPerms.PiShockShareCode;
        dbState.AllowShocks = apiPerms.AllowShocks;
        dbState.AllowVibrations = apiPerms.AllowVibrations;
        dbState.AllowBeeps = apiPerms.AllowBeeps;
        dbState.MaxIntensity = apiPerms.MaxIntensity;
        dbState.MaxDuration = apiPerms.MaxDuration;
        dbState.MaxVibrateDuration = apiPerms.MaxVibrateDuration;

        return dbState;
    }

    public static PairPermAccess ToApi(this PairPermissionAccess? dbState)
    {
        var apiPerms = new PairPermAccess();
        if (dbState is null)
            return apiPerms;

        apiPerms.ChatGarblerActiveAllowed = dbState.ChatGarblerActiveAllowed;
        apiPerms.ChatGarblerLockedAllowed = dbState.ChatGarblerLockedAllowed;
        apiPerms.GaggedNameplateAllowed = dbState.GaggedNameplateAllowed;

        apiPerms.WardrobeEnabledAllowed = dbState.WardrobeEnabledAllowed;
        apiPerms.GagVisualsAllowed = dbState.GagVisualsAllowed;
        apiPerms.RestrictionVisualsAllowed = dbState.RestrictionVisualsAllowed;
        apiPerms.RestraintSetVisualsAllowed = dbState.RestraintSetVisualsAllowed;

        apiPerms.PermanentLocksAllowed = dbState.PermanentLocksAllowed;
        apiPerms.OwnerLocksAllowed = dbState.OwnerLocksAllowed;
        apiPerms.DevotionalLocksAllowed = dbState.DevotionalLocksAllowed;

        apiPerms.ApplyGagsAllowed = dbState.ApplyGagsAllowed;
        apiPerms.LockGagsAllowed = dbState.LockGagsAllowed;
        apiPerms.MaxGagTimeAllowed = dbState.MaxGagTimeAllowed;
        apiPerms.UnlockGagsAllowed = dbState.UnlockGagsAllowed;
        apiPerms.RemoveGagsAllowed = dbState.RemoveGagsAllowed;

        apiPerms.ApplyRestrictionsAllowed = dbState.ApplyRestrictionsAllowed;
        apiPerms.LockRestrictionsAllowed = dbState.LockRestrictionsAllowed;
        apiPerms.MaxRestrictionTimeAllowed = dbState.MaxRestrictionTimeAllowed;
        apiPerms.UnlockRestrictionsAllowed = dbState.UnlockRestrictionsAllowed;
        apiPerms.RemoveRestrictionsAllowed = dbState.RemoveRestrictionsAllowed;

        apiPerms.ApplyRestraintSetsAllowed = dbState.ApplyRestraintSetsAllowed;
        apiPerms.ApplyLayersAllowed = dbState.ApplyLayersAllowed;
        apiPerms.ApplyLayersWhileLockedAllowed = dbState.ApplyLayersWhileLockedAllowed;
        apiPerms.LockRestraintSetsAllowed = dbState.LockRestraintSetsAllowed;
        apiPerms.MaxRestraintTimeAllowed = dbState.MaxRestraintTimeAllowed;
        apiPerms.UnlockRestraintSetsAllowed = dbState.UnlockRestraintSetsAllowed;
        apiPerms.RemoveLayersAllowed = dbState.RemoveLayersAllowed;
        apiPerms.RemoveLayersWhileLockedAllowed = dbState.RemoveLayersWhileLockedAllowed;
        apiPerms.RemoveRestraintSetsAllowed = dbState.RemoveRestraintSetsAllowed;

        apiPerms.PuppeteerEnabledAllowed = dbState.PuppeteerEnabledAllowed;
        apiPerms.PuppetPermsAllowed = dbState.PuppetPermsAllowed;

        apiPerms.MoodlesEnabledAllowed = dbState.MoodlesEnabledAllowed;
        apiPerms.MoodleAccessAllowed = dbState.MoodleAccessAllowed;
        apiPerms.MaxMoodleTimeAllowed = dbState.MaxMoodleTimeAllowed;

        apiPerms.HypnosisMaxTimeAllowed = dbState.HypnosisMaxTimeAllowed;
        apiPerms.HypnosisSendingAllowed = dbState.HypnosisSendingAllowed;

        apiPerms.SpatialAudioAllowed = dbState.SpatialAudioAllowed;
        apiPerms.ExecutePatternsAllowed = dbState.ExecutePatternsAllowed;
        apiPerms.StopPatternsAllowed = dbState.StopPatternsAllowed;
        apiPerms.ToggleAlarmsAllowed = dbState.ToggleAlarmsAllowed;
        apiPerms.ToggleTriggersAllowed = dbState.ToggleTriggersAllowed;

        return apiPerms;
    }

    public static PairPermissionAccess ToModel(this PairPermAccess apiPerms, PairPermissionAccess dbState)
    {
        if (apiPerms is null)
            return dbState;

        // Otherwise update it.
        dbState.ChatGarblerActiveAllowed = apiPerms.ChatGarblerActiveAllowed;
        dbState.ChatGarblerLockedAllowed = apiPerms.ChatGarblerLockedAllowed;
        dbState.GaggedNameplateAllowed = apiPerms.GaggedNameplateAllowed;

        dbState.WardrobeEnabledAllowed = apiPerms.WardrobeEnabledAllowed;
        dbState.GagVisualsAllowed = apiPerms.GagVisualsAllowed;
        dbState.RestrictionVisualsAllowed = apiPerms.RestrictionVisualsAllowed;
        dbState.RestraintSetVisualsAllowed = apiPerms.RestraintSetVisualsAllowed;

        dbState.PermanentLocksAllowed = apiPerms.PermanentLocksAllowed;
        dbState.OwnerLocksAllowed = apiPerms.OwnerLocksAllowed;
        dbState.DevotionalLocksAllowed = apiPerms.DevotionalLocksAllowed;

        dbState.ApplyGagsAllowed = apiPerms.ApplyGagsAllowed;
        dbState.LockGagsAllowed = apiPerms.LockGagsAllowed;
        dbState.MaxGagTimeAllowed = apiPerms.MaxGagTimeAllowed;
        dbState.UnlockGagsAllowed = apiPerms.UnlockGagsAllowed;
        dbState.RemoveGagsAllowed = apiPerms.RemoveGagsAllowed;

        dbState.ApplyRestrictionsAllowed = apiPerms.ApplyRestrictionsAllowed;
        dbState.LockRestrictionsAllowed = apiPerms.LockRestrictionsAllowed;
        dbState.MaxRestrictionTimeAllowed = apiPerms.MaxRestrictionTimeAllowed;
        dbState.UnlockRestrictionsAllowed = apiPerms.UnlockRestrictionsAllowed;
        dbState.RemoveRestrictionsAllowed = apiPerms.RemoveRestrictionsAllowed;

        dbState.ApplyRestraintSetsAllowed = apiPerms.ApplyRestraintSetsAllowed;
        dbState.ApplyLayersAllowed = apiPerms.ApplyLayersAllowed;
        dbState.ApplyLayersWhileLockedAllowed = apiPerms.ApplyLayersWhileLockedAllowed;
        dbState.LockRestraintSetsAllowed = apiPerms.LockRestraintSetsAllowed;
        dbState.MaxRestraintTimeAllowed = apiPerms.MaxRestraintTimeAllowed;
        dbState.UnlockRestraintSetsAllowed = apiPerms.UnlockRestraintSetsAllowed;
        dbState.RemoveLayersAllowed = apiPerms.RemoveLayersAllowed;
        dbState.RemoveLayersWhileLockedAllowed = apiPerms.RemoveLayersWhileLockedAllowed;
        dbState.RemoveRestraintSetsAllowed = apiPerms.RemoveRestraintSetsAllowed;

        dbState.PuppeteerEnabledAllowed = apiPerms.PuppeteerEnabledAllowed;
        dbState.PuppetPermsAllowed = apiPerms.PuppetPermsAllowed;

        dbState.MoodlesEnabledAllowed = apiPerms.MoodlesEnabledAllowed;
        dbState.MoodleAccessAllowed = apiPerms.MoodleAccessAllowed;
        dbState.MaxMoodleTimeAllowed = apiPerms.MaxMoodleTimeAllowed;

        dbState.HypnosisMaxTimeAllowed = apiPerms.HypnosisMaxTimeAllowed;
        dbState.HypnosisSendingAllowed = apiPerms.HypnosisSendingAllowed;

        dbState.SpatialAudioAllowed = apiPerms.SpatialAudioAllowed;
        dbState.ExecutePatternsAllowed = apiPerms.ExecutePatternsAllowed;
        dbState.StopPatternsAllowed = apiPerms.StopPatternsAllowed;
        dbState.ToggleAlarmsAllowed = apiPerms.ToggleAlarmsAllowed;
        dbState.ToggleTriggersAllowed = apiPerms.ToggleTriggersAllowed;
        
        return dbState;
    }
}
#pragma warning restore MA0051
#nullable disable