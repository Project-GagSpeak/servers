using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GagspeakServer.Models;

namespace GagSpeakServer.Models.Permissions;

public class PuppeteerPairPermissions // the unique permissions
{
     [MaxLength(10)] // Composite key with OtherUserUID
     public string UserUID { get; set; }          // the UID of client's user
     public User User { get; set; }               // the user object of the client's user

     [MaxLength(10)] // Composite key with UserUID
     public string OtherUserUID { get; set; }     // the UID of the other user
     public User OtherUser { get; set; }          // the user object of the other user


     // the permissions we set for each client pair:
     public string TriggerPhrase { get; set; }    // the end char that is the right enclosing bracket character for commands.
     public char StartChar { get; set; }          // the start char that is the left enclosing bracket character for commands.
     public char EndChar { get; set; }            // the end char that is the right enclosing bracket character for commands.
     public bool AllowSitRequests { get; set; }   // if the client pair can request to sit on you.
     public bool AllowMotionRequests { get; set; } // if the client pair can request to move you.
     public bool AllowAllRequests { get; set; }   // if the client pair can request to do anything.
}