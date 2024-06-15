using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GagspeakServer.Models;

namespace GagSpeakServer.Models.Permissions;

public class ToyboxPairPermissions // the unique permissions
{
     [MaxLength(10)] // Composite key with OtherUserUID
     public string UserUID { get; set; }          // the UID of client's user
     public User User { get; set; }               // the user object of the client's user

     [MaxLength(10)] // Composite key with UserUID
     public string OtherUserUID { get; set; }     // the UID of the other user
     public User OtherUser { get; set; }          // the user object of the other user

     // the permissions we set for each client pair:
     public bool CanChangeToyState { get; set; }   // if the client pair can turn your toy on and off.
     public bool IntensityControl { get; set; }    // if the client pair can control the intensity of your toy.
     public bool CanUsePatterns { get; set; }      // if the client pair can use patterns on your toy.
     public bool CanUseTriggers { get; set; }      // if the client pair can use triggers on your toy.
}