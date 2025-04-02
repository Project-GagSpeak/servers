using GagspeakAPI.Enums;
using System.ComponentModel.DataAnnotations;

namespace GagspeakShared.Models;

/// <summary>
/// The <c>ClientPairPermissionAccess</c> defines what togglable permissions you wish to give this person access to overwrite.
/// <para> Primary key is a composite key of UserUID and OtherUserUID.</para>
/// <para> This allows USER A to constrict the level of control USER B has for them. </para>
/// </summary>
public class ClientPairPermissionAccess
{
    [MaxLength(10)] // Composite key with OtherUserUID
    public string UserUID { get; set; }          // the UID of client's user
    public User User { get; set; }               // the user object of the client's user


    [MaxLength(10)] // Composite key with UserUID
    public string OtherUserUID { get; set; }     // the UID of the other user
    public User OtherUser { get; set; }          // the user object of the other user

    public bool ChatGarblerActiveAllowed { get; set; } = false; // Global
    public bool ChatGarblerLockedAllowed { get; set; } = false; // Global

    public bool PermanentLocksAllowed { get; set; } = false;
    public bool OwnerLocksAllowed { get; set; } = false;
    public bool DevotionalLocksAllowed { get; set; } = false;

    public bool ApplyGagsAllowed { get; set; } = false;
    public bool LockGagsAllowed { get; set; } = false;
    public bool MaxGagTimeAllowed { get; set; } = false;
    public bool UnlockGagsAllowed { get; set; } = false;
    public bool RemoveGagsAllowed { get; set; } = false;

    // unique permissions for the wardrobe
    public bool WardrobeEnabledAllowed { get; set; } = false; // Global
    public bool GagVisualsAllowed { get; set; } = false; // Global
    public bool RestrictionVisualsAllowed { get; set; } = false; // Global
    public bool RestraintSetVisualsAllowed { get; set; } = false; // Global

    public bool ApplyRestrictionsAllowed { get; set; } = false;
    public bool ApplyRestraintLayersAllowed { get; set; } = false;
    public bool LockRestrictionsAllowed { get; set; } = false;
    public bool MaxRestrictionTimeAllowed { get; set; } = false;
    public bool UnlockRestrictionsAllowed { get; set; } = false;
    public bool RemoveRestrictionsAllowed { get; set; } = false;

    public bool ApplyRestraintSetsAllowed { get; set; } = false;
    public bool LockRestraintSetsAllowed { get; set; } = false;
    public bool MaxRestraintTimeAllowed { get; set; } = false;
    public bool UnlockRestraintSetsAllowed { get; set; } = false;
    public bool RemoveRestraintSetsAllowed { get; set; } = false;

    // unique permissions for the puppeteer
    public bool PuppeteerEnabledAllowed { get; set; } = false; // Global
    public PuppetPerms PuppetPermsAllowed { get; set; } = PuppetPerms.None;

    // Moodles
    public bool MoodlesEnabledAllowed { get; set; } = false; // Global
    public MoodlePerms MoodlePermsAllowed { get; set; } = MoodlePerms.None;
    public bool MaxMoodleTimeAllowed { get; set; } = false;

    // unique permissions for the toybox
    public bool ToyboxEnabledAllowed { get; set; } = false; // Global
    public bool LockToyboxUIAllowed { get; set; } = false; // Global
    public bool SpatialAudioAllowed { get; set; } = false; // Global
    public bool ToggleToyStateAllowed { get; set; } = false;
    public bool RemoteControlAccessAllowed { get; set; } = false;
    public bool ExecutePatternsAllowed { get; set; } = false;
    public bool StopPatternsAllowed { get; set; } = false;
    public bool ToggleAlarmsAllowed { get; set; } = false;
    public bool ToggleTriggersAllowed { get; set; } = false;
}