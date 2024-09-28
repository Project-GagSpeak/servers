using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

public class UserProfileData
{
    // the UserUID is a foreign key to the User table
    // this means that the UserUID must be a primary key in the User table,
    // and also that the UserUID must be unique in the User table
    // (This is also the primary key for this class)
    [Required]
    [Key]
    [ForeignKey(nameof(User))]
    public string UserUID { get; set; }
    public User User { get; set; }

    /* ---------- Profile Data ---------- */
    public string Base64ProfilePic { get; set; } // the base64 string of the user's profile picture
    public string UserDescription { get; set; }  // the user's description.

    public bool FlaggedForReport { get; set; }   // if a profile is flagged for report.
    public bool ProfileDisabled { get; set; }    // If profile is disabled.
    public DateTime ProfileTimeoutTimeStamp { get; set; } // the time the profile was disabled.

    

    // For Profile customization unlocks & progress, we could store a whole other table with lots of unlock categories, or throw a base64 string in here of the class.
}