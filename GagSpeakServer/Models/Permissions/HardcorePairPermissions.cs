using System.ComponentModel.DataAnnotations;
using GagspeakServer.Models;

namespace GagSpeakServer.Models.Permissions;

public class HardcorePairPermissions // the unique permissions
{
     [MaxLength(10)] // Composite key with OtherUserUID
     public string UserUID { get; set; }          // the UID of client's user
     public User User { get; set; }               // the user object of the client's user

     [MaxLength(10)] // Composite key with UserUID
     public string OtherUserUID { get; set; }     // the UID of the other user
     public User OtherUser { get; set; }          // the user object of the other user

     // the permissions we set for each client pair:
     public bool AllowForcedFollow { get; set; } = false;     // if you give player permission
     public bool IsForcedToFollow { get; set; } = false;      // if the player has activated it
     public bool AllowForcedSit { get; set; } = false;        // if you give player permission
     public bool IsForcedToSit { get; set; } = false;         // if the player has activated it 
     public bool AllowForcedToStay { get; set; } = false;     // if you give player permission
     public bool IsForcedToStay { get; set; } = false;        // if the player has activated it
     public bool AllowBlindfold { get; set; } = false;       // if you give player permission
     public bool ForceLockFirstPerson { get; set; } = false; // if you force first person view
     public bool IsBlindfoldeded { get; set; } = false;      // if the player has activated it
}