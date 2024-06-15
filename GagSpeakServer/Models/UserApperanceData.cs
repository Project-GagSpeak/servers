using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Permissions;

namespace GagspeakServer.Models;

public class UserApperanceData // the user's appearance data
{
     public string            SlotOneGagType { get; set; } // the type of the user's first gag
     public string            SlotOneGagPadlock { get; set; } // the padlock of the user's first gag
     public string            SlotOneGagPassword { get; set; } // the password of the user's first gag
     public DateTimeOffset    SlotOneGagTimer { get; set; } // the timer of the user's first gag
     public string            SlotOneGagAssigner { get; set; } // the assigner of the user's first gag

     public string            SlotTwoGagType { get; set; } // the type of the user's second gag
     public string            SlotTwoGagPadlock { get; set; } // the padlock of the user's second gag
     public string            SlotTwoGagPassword { get; set; } // the password of the user's second gag
     public DateTimeOffset    SlotTwoGagTimer { get; set; } // the timer of the user's second gag
     public string            SlotTwoGagAssigner { get; set; } // the assigner of the user's second gag

     public string            SlotThreeGagType { get; set; } // the type of the user's third gag
     public string            SlotThreeGagPadlock { get; set; } // the padlock of the user's third gag
     public string            SlotThreeGagPassword { get; set; } // the password of the user's third gag
     public DateTimeOffset    SlotThreeGagTimer { get; set; } // the timer of the user's third gag
     public string            SlotThreeGagAssigner { get; set; } // the assigner of the user's third gag

     // the UserUID is a foreign key to the User table
     // this means that the UserUID must be a primary key in the User table,
     // and also that the UserUID must be unique in the User table
     // (This is also the primary key for this class)
     [Required]
     [Key]
     [ForeignKey(nameof(User))]
     public string UserUID { get; set; }
}