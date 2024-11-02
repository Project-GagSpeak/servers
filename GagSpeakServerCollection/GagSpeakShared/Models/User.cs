using System.ComponentModel.DataAnnotations;
using GagspeakAPI.Enums;

namespace GagspeakShared.Models;

/// <summary> Represents a user profile in the system. (will only ever link to one playercharacter)</summary>
public class User
{
    [Key]
    [MaxLength(10)]
    public string UID { get; set; }                 // The primary key for the User

    public DateTime CreatedDate { get; set; }       // the timestamp of the user's creation in UTC format.

    public DateTime LastLoggedIn { get; set; }      // Last time the user logged in.

    [MaxLength(15)]
    public string Alias { get; set; }               // Alias that the person sets for the user's UID, (Patreon reward)

    public CkSupporterTier VanityTier { get; set; } = CkSupporterTier.NoRole; // The vanity tier of the user

    public bool ProfileReportingTimedOut { get; set; } // If the user's profile is currently timed out from reporting
    public int UploadLimitCounter { get; set; } = 0; // Counter for uploads in the current week (should be 10 by default)
    public DateTime FirstUploadTimestamp { get; set; } = DateTime.MinValue; // Timestamp of the first upload in the current week

}
