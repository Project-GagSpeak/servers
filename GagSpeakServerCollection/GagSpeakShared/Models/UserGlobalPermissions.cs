using GagspeakAPI.Attributes;
using GagspeakAPI.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

public class UserGlobalPermissions
{
    public InptChannel AllowedGarblerChannels      { get; set; } = InptChannel.None; 
    public bool        ChatGarblerActive           { get; set; } = false; // if the live chat garbler is active
    public bool        ChatGarblerLocked           { get; set; } = false; // if the live chat garbler is locked in an active state.

    // wardrobe global modifiable permissions
    public bool        WardrobeEnabled             { get; set; } = false; // PROFILE VIEWABLE OPT-IN || If the user's wardrobe component is active
    public bool        GagVisuals                  { get; set; } = false; // Determines if any visual alterations of gags are applied.
    public bool        RestrictionVisuals          { get; set; } = false; // Determines if any visual alterations of restrictions are applied.
    public bool        RestraintSetVisuals         { get; set; } = false; // Determines if any visual alterations of restraint sets are applied.


    // global puppeteer modifiable permissions.
    public bool        PuppeteerEnabled            { get; set; } = false; // PROFILE VIEWABLE OPT-IN || If the user's puppeteer component is active
    public string      TriggerPhrase               { get; set; } = string.Empty; // PROFILE VIEWABLE OPT-IN || Global trigger phrase for the user
    public PuppetPerms PuppetPerms                 { get; set; } = PuppetPerms.None;

    // global toybox modifiable permissions
    public bool        ToyboxEnabled               { get; set; } = false; // PROFILE VIEWABLE OPT-IN || If the user's toybox component is active
    public bool        LockToyboxUI                { get; set; } = false; // Prevents the user from interfacing with their connected toys.
    public bool        ToysAreConnected            { get; set; } = false; // If any toys are connected (True if simulated by default)
    public bool        ToysAreInUse                { get; set; } = false; // True if connected to any remote / vibe room.
    public bool        SpatialAudio                { get; set; } = false; // if the user's toybox local audio is active

    // global hardcore permissions (readonly for everyone)
    // Contains the UID who applied it when active. If Devotional, will have    |pairlocked    appended.
    public string      ForcedFollow                { get; set; } = string.Empty;
    public string      ForcedEmoteState            { get; set; } = string.Empty;
    public string      ForcedStay                  { get; set; } = string.Empty;
    public string      ChatBoxesHidden             { get; set; } = string.Empty;
    public string      ChatInputHidden             { get; set; } = string.Empty;
    public string      ChatInputBlocked            { get; set; } = string.Empty;
    public string      HypnosisCustomEffect        { get; set; } = string.Empty;

    // Global PiShock Permissions & Helpers.
    public string      GlobalShockShareCode        { get; set; } = string.Empty;
    public bool        AllowShocks                 { get; set; } = false;
    public bool        AllowVibrations             { get; set; } = false;
    public bool        AllowBeeps                  { get; set; } = false;
    public int         MaxIntensity                { get; set; } = -1;
    public int         MaxDuration                 { get; set; } = -1;
    public TimeSpan    ShockVibrateDuration        { get; set; } = TimeSpan.Zero;
    
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