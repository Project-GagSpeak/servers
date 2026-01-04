using System.ComponentModel.DataAnnotations;
using GagspeakAPI.Enums;

namespace GagspeakShared.Models;

/// <summary> Represents a user profile in the system. </summary>
public class User
{
    [Key]
    [MaxLength(10)]
    public string           UID         { get; set; } // The primary key for the User

    [MaxLength(15)]
    public string           Alias       { get; set; } // Alias that the person sets for the user's UID, (Patreon reward)
    public DateTime         CreatedAt   { get; set; } // the timestamp of the user's creation in UTC format.
    public DateTime         LastLogin   { get; set; } // Last time the user logged in.
    public CkSupporterTier  Tier        { get; set; } = CkSupporterTier.NoRole; // The vanity tier of the user
}
