using GagspeakAPI.Attributes;
using GagspeakAPI.Data;
using GagspeakAPI.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

/// <summary> Pray. </summary>
public class UserRestraintData : IPadlockableRestriction
{
    [Key]
    public string UserUID { get; set; }

    [ForeignKey(nameof(UserUID))]
    public virtual User User { get; set; }

    public Guid Identifier { get; set; } = Guid.Empty;

    [MaxLength(10)]
    public string Enabler { get; set; } = string.Empty;
    public Padlocks Padlock { get; set; } = Padlocks.None;

    [MaxLength(20)]
    public string Password { get; set; } = string.Empty;
    public DateTimeOffset Timer { get; set; } = DateTimeOffset.MinValue;

    [MaxLength(10)]
    public string PadlockAssigner { get; set; } = string.Empty;

    // Layering on a RestraintSet
    public virtual ICollection<UserRestraintLayer> Layers { get; set; } = [];

    // Base RestraintSet Properties

    public ApplyModifiers Modifiers { get; set; } = ApplyModifiers.None;

    // Hardcore Escape
    public byte Tightness { get; set; } = 0;
    public byte Integrity { get; set; } = 0;
    public byte Rigidity { get; set; } = 0;
    public byte Fragility { get; set; } = 0;
    public byte Complexity { get; set; } = 0;

    public byte[] Dyes { get; set; } = new byte[20];

    // Any possible number of mod presets.

    public bool IsLocked() => Padlock != Padlocks.None;
    public bool HasTimerExpired() => DateTimeOffset.UtcNow >= Timer;
}

public class UserRestraintLayer
{
    [Key]
    [Column(Order = 0)]
    public string UserUID { get; set; }

    [ForeignKey(nameof(UserUID))]
    public virtual User User { get; set; }

    [Key]
    [Column(Order = 1)]
    public byte Layer { get; set; } = 0;

    // Points to Restriction Identifier
    public Guid Identifier { get; set; } = Guid.Empty;

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
}