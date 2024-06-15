using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GagSpeakServer.Models.Permissions;

namespace GagspeakServer.Models;

/// <summary>
/// The <c>ClientPairPermissions</c> class defines what unique permissions a user has with another paired user.
/// <para> Primary key is a composite key of UserUID and OtherUserUID.</para>
/// <para> This allows USER A to contain permissions for USER B, and for USER B to contain permissions for USER A </para
/// </summary>
public class ClientPairPermissions
{
     [MaxLength(10)] // Composite key with OtherUserUID
     public string UserUID { get; set; }          // the UID of client's user
     public User User { get; set; }               // the user object of the client's user


     [MaxLength(10)] // Composite key with UserUID
     public string OtherUserUID { get; set; }     // the UID of the other user
     public User OtherUser { get; set; }          // the user object of the other user

     // unique permissions stored here:
     public bool ExtendedLockTimes { get; set; }  // if the client pair can lock you for extended times
     public bool InHardcore { get; set; }         // displays if you have decided to enter hardcore mode with this user (grants hardcore module)
     public WardrobePairPermissions WardrobePairPermissions { get; set; } // the wardrobe permissions for this pair
     public PuppeteerPairPermissions PuppeteerPairPermissions { get; set; } // the puppeteer permissions for this pair
     public ToyboxPairPermissions ToyboxPairPermissions { get; set; } // the toybox permissions for this pair
     public HardcorePairPermissions HardcorePairPermissions { get; set; } // the hardcore permissions for this pair
}




