using GagspeakAPI.Attributes;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

public class UserGlobalPermissions : IReadOnlyGlobalPerms
{
    public InptChannel AllowedGarblerChannels      { get; set; } = InptChannel.None; 
    public bool        ChatGarblerActive           { get; set; } = false;
    public bool        ChatGarblerLocked           { get; set; } = false;
    public bool        GaggedNameplate             { get; set; } = false;

    // wardrobe global modifiable permissions
    public bool        WardrobeEnabled             { get; set; } = false;
    public bool        GagVisuals                  { get; set; } = false;
    public bool        RestrictionVisuals          { get; set; } = false;
    public bool        RestraintSetVisuals         { get; set; } = false;


    // global puppeteer modifiable permissions.
    public bool        PuppeteerEnabled            { get; set; } = false;
    public string      TriggerPhrase               { get; set; } = string.Empty;
    public PuppetPerms PuppetPerms                 { get; set; } = PuppetPerms.None;

    // global toybox modifiable permissions
    public bool        ToyboxEnabled               { get; set; } = false;
    public bool        ToysAreInteractable         { get; set; } = false;
    public bool        InVibeRoom                  { get; set; } = false;
    public bool        SpatialAudio                { get; set; } = false;

    // Going to need to fine tune this soon, but its purpose is to stop others
    // from applying effects while a restriction or other player has one active.
    public string      HypnosisCustomEffect        { get; set; } = string.Empty;

    // global hardcore permissions (readonly for everyone)
    // Contains the UID who applied it when active. If Devotional, will have    |pairlocked    appended.
    public string      LockedFollowing             { get; set; } = string.Empty;
    public string      LockedEmoteState            { get; set; } = string.Empty;
    public string      IndoorConfinement           { get; set; } = string.Empty;
    public string      Imprisonment                { get; set; } = string.Empty;
    public string      ChatBoxesHidden             { get; set; } = string.Empty;
    public string      ChatInputHidden             { get; set; } = string.Empty;
    public string      ChatInputBlocked            { get; set; } = string.Empty;

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