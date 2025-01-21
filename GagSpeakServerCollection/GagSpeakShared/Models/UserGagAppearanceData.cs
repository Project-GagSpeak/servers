using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

public class UserGagGagData // the user's appearance data
{
    [Required]
    [Key]
    [ForeignKey(nameof(User))]
    public string UserUID { get; set; }
    public User User { get; set; }


    public string           SlotOneGagType { get; set; } = "None";// the type of the user's first gag
    public string           SlotOneGagPadlock { get; set; } = "None"; // the padlock of the user's first gag
    public string           SlotOneGagPassword { get; set; } = "";// the password of the user's first gag
    public DateTimeOffset   SlotOneGagTimer { get; set; } = DateTimeOffset.UtcNow; // the timer of the user's first gag
    public string           SlotOneGagAssigner { get; set; } = "";// the assigner of the user's first gag

    public string           SlotTwoGagType { get; set; } = "None";// the type of the user's second gag
    public string           SlotTwoGagPadlock { get; set; } = "None"; // the padlock of the user's second gag
    public string           SlotTwoGagPassword { get; set; } = ""; // the password of the user's second gag
    public DateTimeOffset   SlotTwoGagTimer { get; set; } = DateTimeOffset.UtcNow; // the timer of the user's second gag
    public string           SlotTwoGagAssigner { get; set; } = ""; // the assigner of the user's second gag

    public string           SlotThreeGagType { get; set; } = "None"; // the type of the user's third gag
    public string           SlotThreeGagPadlock { get; set; } = "None"; // the padlock of the user's third gag
    public string           SlotThreeGagPassword { get; set; } = ""; // the password of the user's third gag
    public DateTimeOffset   SlotThreeGagTimer { get; set; } = DateTimeOffset.UtcNow; // the timer of the user's third gag
    public string           SlotThreeGagAssigner { get; set; } = ""; // the assigner of the user's third gag
}