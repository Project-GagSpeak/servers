using System.ComponentModel.DataAnnotations;

namespace GagspeakServer.Models;

/// <summary> Represents a user profile in the system. (will only ever link to one playercharacter)</summary>
public class User
{
    [Key]
    [MaxLength(10)]
    public string UID { get; set; }                 // The primary key for the User

    [Timestamp]
    public byte[] Timestamp { get; set; }           // the timestamp of the user's creation

    public bool IsModerator { get; set; } = false;  // If the user is a moderator (should remove since this is just for syncshell stuff???

    public bool IsAdmin { get; set; } = false;      // same as above

    public DateTime LastLoggedIn { get; set; }      // Last time the user logged in.

    [MaxLength(15)]
    public string Alias { get; set; }               // Alias that the person sets for the user's UID, (Patreon reward)
}
