using GagspeakAPI.Data.Interfaces;
using GagspeakAPI.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;
public class UserRestrictionData : IPadlockableRestriction
{
    [Key]
    [Column(Order = 0)]
    public string UserUID { get; set; }
    public virtual User User { get; set; }

    [Key]
    [Column(Order = 1)]
    public byte           Layer           { get; set; } = 0;

    public Guid           Identifier   { get; set; } = Guid.Empty;

    [MaxLength(10)]
    public string         Enabler         { get; set; } = string.Empty;
    public Padlocks       Padlock         { get; set; } = Padlocks.None;

    [MaxLength(20)]
    public string         Password        { get; set; } = string.Empty;
    public DateTimeOffset Timer           { get; set; } = DateTimeOffset.MinValue;

    [MaxLength(10)]
    public string         PadlockAssigner { get; set; } = string.Empty;

    public bool IsLocked() => Padlock != Padlocks.None;
    public bool HasTimerExpired() => DateTimeOffset.UtcNow >= Timer;
}