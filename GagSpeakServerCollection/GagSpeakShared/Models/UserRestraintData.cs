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
    // a bitfield representing which layers are active '1' and which are inactive '0', 5 bits : 0b00000
    public RestraintLayer ActiveLayers { get; set; } = RestraintLayer.None;

    [MaxLength(10)]
    public string Enabler { get; set; } = string.Empty;
    public Padlocks Padlock { get; set; } = Padlocks.None;

    [MaxLength(20)]
    public string Password { get; set; } = string.Empty;
    public DateTimeOffset Timer { get; set; } = DateTimeOffset.MinValue;

    [MaxLength(10)]
    public string PadlockAssigner { get; set; } = string.Empty;

    public bool IsLocked() => Padlock != Padlocks.None;
    public bool HasTimerExpired() => DateTimeOffset.UtcNow >= Timer;
}