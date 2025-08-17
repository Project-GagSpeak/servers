using GagspeakAPI.Data;
using GagspeakAPI.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

/*public class UserGagData2
{
    [Required]
    [Key]
    [ForeignKey(nameof(User))]
    public string UserUID { get; set; }
    public User User { get; set; }

    // Slot One
    public GagType        GagOne        { get; set; } = GagType.None;
    public Padlocks       PadlockOne    { get; set; } = Padlocks.None;
    [MaxLength(20)]
    public string         PasswordOne   { get; set; } = string.Empty;
    public DateTimeOffset TimerOne      { get; set; } = DateTimeOffset.MinValue;
    [MaxLength(10)]
    public string         AssignerOne   { get; set; } = string.Empty;

    // Slot Two
    public GagType        GagTwo        { get; set; } = GagType.None;
    public Padlocks       PadlockTwo    { get; set; } = Padlocks.None;
    [MaxLength(20)]
    public string         PasswordTwo   { get; set; } = string.Empty;
    public DateTimeOffset TimerTwo      { get; set; } = DateTimeOffset.MinValue;
    [MaxLength(10)]
    public string         AssignerTwo   { get; set; } = string.Empty;

    // Slot Three
    public GagType        GagThree      { get; set; } = GagType.None;
    public Padlocks       PadlockThree  { get; set; } = Padlocks.None;
    [MaxLength(20)]
    public string         PasswordThree { get; set; } = string.Empty;
    public DateTimeOffset TimerThree    { get; set; } = DateTimeOffset.MinValue;
    [MaxLength(10)]
    public string         AssignerThree { get; set; } = string.Empty;
}*/

public class UserGagData : IPadlockableRestriction
{
    [Key]
    [Column(Order = 0)]
    public string UserUID { get; set; }

    [ForeignKey(nameof(UserUID))]
    public virtual User User { get; set; }

    [Key]
    [Column(Order = 1)]
    public byte           Layer           { get; set; } = 0;

    public GagType        Gag             { get; set; } = GagType.None;

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