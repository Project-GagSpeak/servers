using GagspeakAPI.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

public class UserGlobalPermissions
{
    // main global permissions
    public string Safeword { get; set; } = "NONE SET";          // DO NOT ALLOW THIS TO BE MODIFABLE
    public bool SafewordUsed { get; set; } = false;             // DO NOT ALLOW THIS TO BE MODIFABLE
    public bool HardcoreSafewordUsed { get; set; } = false;     // DO NOT ALLOW THIS TO BE MODIFABLE
    public bool LiveChatGarblerActive { get; set; } = false;    // if the live chat garbler is active
    public bool LiveChatGarblerLocked { get; set; } = false;    // if the live chat garbler is locked in an active state.


    // wardrobe global modifiable permissions
    public bool WardrobeEnabled { get; set; } = false;          // PROFILE VIEWABLE OPT-IN || If the user's wardrobe component is active
    public bool ItemAutoEquip { get; set; } = false;            // if the user allows items to be auto-equipped
    public bool RestraintSetAutoEquip { get; set; } = false;    // if the user allows restraint sets to be auto-equipped


    // global puppeteer modifiable permissions.
    public bool PuppeteerEnabled { get; set; } = false;         // PROFILE VIEWABLE OPT-IN || If the user's puppeteer component is active
    public string GlobalTriggerPhrase { get; set; } = "";       // PROFILE VIEWABLE OPT-IN || Global trigger phrase for the user
    public bool GlobalAllowSitRequests { get; set; } = false;   // PROFILE VIEWABLE OPT-IN || If user allows sit requests
    public bool GlobalAllowMotionRequests { get; set; } = false;// PROFILE VIEWABLE OPT-IN || If the user allows motion requests
    public bool GlobalAllowAllRequests { get; set; } = false;   // PROFILE VIEWABLE OPT-IN || READONLY || If the user allows all requests

    // global moodles modifiable permissions
    public bool MoodlesEnabled { get; set; } = false;           // PROFILE VIEWABLE OPT-IN || If the user's moodles component is active


    // global toybox modifiable permissions
    public bool ToyboxEnabled { get; set; } = false;            // PROFILE VIEWABLE OPT-IN || If the user's toybox component is active
    public bool LockToyboxUI { get; set; } = false;             // if the user's toybox UI is locked
    public bool ToyIsActive { get; set; } = false;              // if the user's toy is active
    public bool SpatialVibratorAudio { get; set; } = false;    // if the user's toybox local audio is active

    // global hardcore permissions (only modifiable by GlobalPerm User)
    public string GlobalShockShareCode { get; set; } = "";
    public TimeSpan GlobalShockVibrateDuration { get; set; } = TimeSpan.Zero;

    // global hardcore permissions (readonly for everyone)
    // Contains the UID who applied it when active. If Devotional, will have    |pairlocked    appended.
    public string ForcedFollow { get; set; } = string.Empty;
    public string ForcedEmoteState { get; set; } = string.Empty; // Format: UID|EmoteID|CposeByte|pairlocked
    public string ForcedStay { get; set; } = string.Empty;
    public string ForcedBlindfold { get; set; } = string.Empty;
    public string ChatboxesHidden { get; set; } = string.Empty;
    public string ChatInputHidden { get; set; } = string.Empty;
    public string ChatInputBlocked { get; set; } = string.Empty;

    public User User { get; set; }
    // the UserUID is a foreign key to the User table
    // this means that the UserUID must be a primary key in the User table,
    // and also that the UserUID must be unique in the User table
    // (This is also the primary key for this class)
    [Required]
    [Key]
    [ForeignKey(nameof(User))]
    public string UserUID { get; set; }
}