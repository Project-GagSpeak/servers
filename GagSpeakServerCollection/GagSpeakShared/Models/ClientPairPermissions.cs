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

    // unique permissions stored here:
    public bool IsPaused { get; set; } = false;  // if the pair is paused, a unique unmodifiable permission by other pairs.
    public bool GagFeatures { get; set; } = false; // if this pair can apply / lock (/ unlock / remove ??) gag features on you.
    public bool OwnerLocks { get; set; } = false; // if the pair can use OwnerPadlocks & Timer variants. Only the others with this permission can remove them.
    public bool DevotionalLocks { get; set; } = false; // if the pair can use Devotional Padlocks & Timer variants. Only the assigner of these locks can remove them.
    public bool ExtendedLockTimes { get; set; } = false;  // if user allowed extended lock times for this paired user
    public TimeSpan MaxLockTime { get; set; } = TimeSpan.Zero;    // the max lock time for this paired user
    public bool InHardcore { get; set; } = false;         // if the user is in hardcore mode with this paired user

    // unique permissions for the wardrobe
    public bool ApplyRestraintSets { get; set; } = false; // if the client pair can apply your restraint sets.
    public bool LockRestraintSets { get; set; } = false;  // if the client pair can lock your restraint sets
    public TimeSpan MaxAllowedRestraintTime { get; set; } = TimeSpan.Zero; // the max time the client pair can lock your restraint sets
    public bool UnlockRestraintSets { get; set; } = false; // if the client pair can unlock your restraint sets
    public bool RemoveRestraintSets { get; set; } = false; // if the client pair can remove your restraint sets.

    // unique permissions for the puppeteer
    public string TriggerPhrase { get; set; } = "";    // the end char that is the right enclosing bracket character for commands.
    public char StartChar { get; set; } = '(';          // the start char that is the left enclosing bracket character for commands.
    public char EndChar { get; set; } = ')';            // the end char that is the right enclosing bracket character for commands.
    public bool AllowSitRequests { get; set; } = false;   // if the client pair can request to sit on you.
    public bool AllowMotionRequests { get; set; } = false; // if the client pair can request to move you.
    public bool AllowAllRequests { get; set; } = false;   // if the client pair can request to do anything.

    // unique Moodles permissions
    public bool AllowPositiveStatusTypes { get; set; } = false; // if the client pair can give you positive moodles
    public bool AllowNegativeStatusTypes { get; set; } = false; // if the client pair can give you negative moodles
    public bool AllowSpecialStatusTypes { get; set; } = false;  // if the client pair can give you neutral moodles
    public bool PairCanApplyOwnMoodlesToYou { get; set; } = false; // if the client pair can apply their moodles to you
    public bool PairCanApplyYourMoodlesToYou { get; set; } = false; // if the client pair can apply your moodles
    public TimeSpan MaxMoodleTime { get; set; } = TimeSpan.Zero; // the max time the client pair can apply moodles to you
    public bool AllowPermanentMoodles { get; set; } = false; // if the client pair can apply permanent moodles to you
    public bool AllowRemovingMoodles { get; set; } = false; // if the client pair can remove moodles from you.

    // unique permissions for the toybox
    public bool CanToggleToyState { get; set; } = false;   // if the client pair can turn your toy on and off.
    public bool CanUseVibeRemote { get; set; } = false; // if the client pair can use the realtime vibe remote on your toy.
    public bool CanToggleAlarms { get; set; } = false; // if the client pair can toggle alarms on your toy.
    public bool CanSendAlarms { get; set; } = false; // if the client pair can send alarms to your toy.
    public bool CanExecutePatterns { get; set; } = false; // if the client pair can use patterns on your toy.
    public bool CanStopPatterns { get; set; } = false; // if the client pair can stop patterns on your toy.
    public bool CanToggleTriggers { get; set; } = false; // if the client pair can use triggers on your toy.
    public string ShockCollarShareCode { get; set; } = ""; // the share Code for the shock collar unique to this user.
    public TimeSpan MaxVibrateDuration { get; set; } = TimeSpan.Zero; // separate value since vibrations have diff limits.

    // unique hardcore permissions.
    public bool AllowForcedFollow { get; set; } = false;     // if you give player permission
    public bool IsForcedToFollow { get; set; } = false;      // if the player has activated it
    public bool AllowForcedSit { get; set; } = false;        // if you give player permission
    public bool IsForcedToSit { get; set; } = false;         // if the player has activated it 
    public bool AllowForcedToStay { get; set; } = false;     // if you give player permission
    public bool IsForcedToStay { get; set; } = false;        // if the player has activated it
    public bool AllowBlindfold { get; set; } = false;       // if you give player permission
    public bool ForceLockFirstPerson { get; set; } = false; // if you force first person view
    public bool IsBlindfolded { get; set; } = false;      // if the player has activated it
}