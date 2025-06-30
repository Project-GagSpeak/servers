using GagspeakAPI.Enums;
using GagspeakAPI.Extensions;
using System.ComponentModel.DataAnnotations;

namespace GagspeakShared.Models;

/// <summary>
/// The <c>ClientPairPermissions</c> class defines what unique permissions a user has with another paired user.
/// <para> Primary key is a composite key of UserUID and OtherUserUID.</para>
/// <para> This allows USER A to contain permissions for USER B, and for USER B to contain permissions for USER A </para
/// </summary>
public class ClientPairPermissions
{
    [MaxLength(10)] // Composite key with OtherUserUID
    public string UserUID { get; set; }          // the UID of client's user
    public User User { get; set; }               // the user object of the client's user


    [MaxLength(10)] // Composite key with UserUID
    public string OtherUserUID { get; set; }     // the UID of the other user
    public User OtherUser { get; set; }          // the user object of the other user

    public bool        IsPaused                  { get; set; } = false;  // if the pair is paused, a unique unmodifiable permission by other pairs.

    // Advanced Lock Permissions
    public bool        PermanentLocks            { get; set; } = false; // if the client pair can apply permanent gags to you.
    public bool        OwnerLocks                { get; set; } = false; // if the pair can use OwnerPadlocks & Timer variants. Only the others with this permission can remove them.
    public bool        DevotionalLocks           { get; set; } = false; // if the pair can use Devotional Padlocks & Timer variants. Only the assigner of these locks can remove them.

    // Gag Permissions
    public bool        ApplyGags                 { get; set; } = false;
    public bool        LockGags                  { get; set; } = false;
    public TimeSpan    MaxGagTime                { get; set; } = TimeSpan.Zero;
    public bool        UnlockGags                { get; set; } = false;
    public bool        RemoveGags                { get; set; } = false;

    // Restriction Permissions
    public bool        ApplyRestrictions         { get; set; } = false;
    public bool        LockRestrictions          { get; set; } = false;
    public TimeSpan    MaxRestrictionTime        { get; set; } = TimeSpan.Zero;
    public bool        UnlockRestrictions        { get; set; } = false;
    public bool        RemoveRestrictions        { get; set; } = false;

    // unique permissions for the wardrobe
    public bool        ApplyRestraintSets        { get; set; } = false;
    public bool        ApplyLayers               { get; set; } = false;
    public bool        ApplyLayersWhileLocked    { get; set; } = false;
    public bool        LockRestraintSets         { get; set; } = false;
    public TimeSpan    MaxRestraintTime          { get; set; } = TimeSpan.Zero;
    public bool        UnlockRestraintSets       { get; set; } = false;
    public bool        RemoveLayers              { get; set; } = false;
    public bool        RemoveLayersWhileLocked   { get; set; } = false;
    public bool        RemoveRestraintSets       { get; set; } = false;

    // unique permissions for the puppeteer
    public string      TriggerPhrase             { get; set; } = "";    // the end char that is the right enclosing bracket character for commands.
    public char        StartChar                 { get; set; } = '(';   // the start char that is the left enclosing bracket character for commands.
    public char        EndChar                   { get; set; } = ')';   // the end char that is the right enclosing bracket character for commands.
    public PuppetPerms PuppetPerms               { get; set; } = PuppetPerms.None;

    // unique Moodles permissions
    public MoodlePerms MoodlePerms               { get; set; } = MoodlePerms.None;     // Various Moodle Permissions configured through a flag enum.
    public TimeSpan    MaxMoodleTime             { get; set; } = TimeSpan.Zero;

    // unique permissions for the toybox
    public bool        ToggleToyState            { get; set; } = false; // If True, this pair can toggle your toys states.
    public bool        RemoteControlAccess       { get; set; } = false; // If True, this pair can connect a remote to your toys.
    public bool        ExecutePatterns           { get; set; } = false;
    public bool        StopPatterns              { get; set; } = false;
    public bool        ToggleAlarms              { get; set; } = false;
    public bool        ToggleTriggers            { get; set; } = false;

    // Misc.
    public bool        HypnoEffectSending        { get; set; } = false;

    // unique hardcore permissions. (only allow the ALLOW permissions to be set by the user).
    public bool        InHardcore                { get; set; } = false;
    public bool        PairLockedStates          { get; set; } = false; // Treats any State toggled by this pair like a Devotional Padlock.
    public bool        AllowForcedFollow         { get; set; } = false;
    public bool        AllowForcedSit            { get; set; } = false;
    public bool        AllowForcedEmote          { get; set; } = false;
    public bool        AllowForcedStay           { get; set; } = false;
    public bool        AllowGarbleChannelEditing { get; set; } = false;
    public bool        AllowHidingChatBoxes      { get; set; } = false;
    public bool        AllowHidingChatInput      { get; set; } = false;
    public bool        AllowChatInputBlocking    { get; set; } = false;
    public bool        AllowHypnoImageSending    { get; set; } = false;

    public string      PiShockShareCode          { get; set; } = ""; // the share Code for the shock collar unique to this user.
    public bool        AllowShocks               { get; set; } = false; // If we allow shocks from this pair.
    public bool        AllowVibrations           { get; set; } = false; // If we allow vibrations from this pair.
    public bool        AllowBeeps                { get; set; } = false; // If we allow beeps from this pair.
    public int         MaxIntensity              { get; set; } = -1; // the max intensity of the shock, vibration, or beep.
    public int         MaxDuration               { get; set; } = -1; // the max duration of the shock, vibration, or beep.
    public TimeSpan    MaxVibrateDuration        { get; set; } = TimeSpan.Zero; // separate value since vibrations have diff limits.

    // member helper for PiShock functions.
    public bool HasValidShareCode() => !PiShockShareCode.NullOrEmpty() && MaxDuration > 0;
}