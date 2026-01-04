using GagspeakAPI.Attributes;
using GagspeakAPI.Data.Permissions;
using GagspeakAPI.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

public class GlobalPermissions : IReadOnlyGlobalPerms
{
    [Key]
    public string UserUID { get; set; }

    [ForeignKey(nameof(UserUID))]
    public virtual User User { get; set; }

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

    // Global PiShock Permissions & Helpers.
    public string      GlobalShockShareCode        { get; set; } = string.Empty;
    public bool        AllowShocks                 { get; set; } = false;
    public bool        AllowVibrations             { get; set; } = false;
    public bool        AllowBeeps                  { get; set; } = false;
    public int         MaxIntensity                { get; set; } = -1;
    public int         MaxDuration                 { get; set; } = -1;
    public TimeSpan    ShockVibrateDuration        { get; set; } = TimeSpan.Zero;
}