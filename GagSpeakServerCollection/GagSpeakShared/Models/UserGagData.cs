using GagspeakAPI.Data;
using GagspeakAPI.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

public class UserGagData : IPadlockableRestriction
{
    [Key]
    [Column(Order = 0)]
    public string UserUID { get; set; }

    [ForeignKey(nameof(UserUID))]
    public virtual User User { get; set; }

    [Key]
    [Column(Order = 1)]
    public byte Layer { get; set; } = 0;
    
    // Item ID
    public GagType Gag { get; set; } = GagType.None;

    // Application & Lock Details
    [MaxLength(10)]
    public string Enabler { get; set; } = string.Empty;
    public Padlocks Padlock { get; set; } = Padlocks.None;

    [MaxLength(20)] 
    public string Password { get; set; } = string.Empty;
    public DateTimeOffset Timer { get; set; } = DateTimeOffset.MinValue;

    [MaxLength(10)]
    public string PadlockAssigner { get; set; } = string.Empty;

    // Properties
    public byte Dye1 { get; set; } = 0;
    public byte Dye2 { get; set; } = 0;
    public string ModDirectory { get; set; } = string.Empty;
    public string ModName { get; set; } = string.Empty; // May not be nessisary, but penumbra uses a composite key
    public Guid ModPreset { get; set; } = Guid.Empty;
    public ApplyModifiers Modifiers { get; set; } = ApplyModifiers.None;

    // Hardcore Escape
    public byte Tightness { get; set; } = 0;
    public byte Integrity { get; set; } = 0;
    public byte Rigidity { get; set; } = 0;
    public byte Fragility { get; set; } = 0;
    public byte Complexity { get; set; } = 0;

    public bool IsLocked() => Padlock != Padlocks.None;
    public bool HasTimerExpired() => DateTimeOffset.UtcNow >= Timer;
}

// Used to determine which parts of an active item are to be used.
[Flags]
public enum ApplyModifiers : short
{
    None        = 0 << 0,
    DoEmote     = 1 << 0,
    FirstPerson = 1 << 1,
    ShowOverlay = 1 << 2,
    Arousal     = 1 << 3,
    HCTraits    = 1 << 4,

    // Hardcore Escape attribute should be globally enabled and not added here.
}