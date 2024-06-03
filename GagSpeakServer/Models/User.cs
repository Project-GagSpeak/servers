using System.ComponentModel.DataAnnotations;

namespace GagspeakServer.Models;

/// <summary> Represents a user in the system. </summary>
public class User
{
    [Key]
    [MaxLength(10)]
    public string UID { get; set; }                 // the UID of this user. Max of 10 characters

    [Timestamp]
    public byte[] Timestamp { get; set; }           // unknown purpose

    public bool IsModerator { get; set; } = false;  // Is the user a moderator?

    public bool IsAdmin { get; set; } = false;      // Is the user an admin?

    public DateTime LastLoggedIn { get; set; }      // Last time the user logged in.

    [MaxLength(15)]
    public string Alias { get; set; }               // Alias that the person sets for the user's UID, if they can afford it.
}
