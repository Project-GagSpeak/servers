using System.ComponentModel.DataAnnotations;
using GagspeakAPI.Enums;

namespace GagspeakShared.Models;

/// <summary> Represents a user profile in the system. </summary>
public class User
{
    [Key]
    [MaxLength(10)]
    public string UID                       { get; set; } // The primary key for the User

    [MaxLength(15)]
    public string Alias                     { get; set; } // Alias that the person sets for the user's UID, (Patreon reward)
    public DateTime CreatedDate             { get; set; } // the timestamp of the user's creation in UTC format.
    public DateTime LastLoggedIn            { get; set; } // Last time the user logged in.
    public CkSupporterTier VanityTier       { get; set; } = CkSupporterTier.NoRole; // The vanity tier of the user
    public bool Verified                    { get; set; } = false; // Accounts are verified after registering with the discord bot.
    public int UploadLimitCounter           { get; set; } = 0; // Tracks how many uploads have been made.
    public DateTime FirstUploadTimestamp    { get; set; } = DateTime.MinValue; // Timestamp of the first upload in the current week (might be able to remove later idk)

}
