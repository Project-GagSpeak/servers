using System.ComponentModel.DataAnnotations;
using GagspeakAPI.Data.Enum;

namespace GagspeakShared.Models;

/// <summary> Represents a user profile in the system. (will only ever link to one playercharacter)</summary>
public class User
{
    [Key]
    [MaxLength(10)]
    public string UID { get; set; }                 // The primary key for the User

    [Timestamp]
    public byte[] Timestamp { get; set; }           // the timestamp of the user's creation

    public DateTime LastLoggedIn { get; set; }      // Last time the user logged in.

    [MaxLength(15)]
    public string Alias { get; set; }               // Alias that the person sets for the user's UID, (Patreon reward)

    public CkSupporterTier VanityTier { get; set; } // The vanity tier of the user (if a CK supporter)
}
