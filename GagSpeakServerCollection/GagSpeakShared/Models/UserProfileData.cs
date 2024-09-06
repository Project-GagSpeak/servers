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
    public bool FlaggedForReport { get; set; }   // TODO: / REFLECT: Flagged profiles should be tagged as something to lookup for review when reported.
    public bool ProfileDisabled { get; set; }    // If profile is disabled. 
    public string UserDescription { get; set; }  // the user's description

    // TODO: Add customization / Cosmetic feature presets below here:
    // (prefer to not store cosmetics actually since they are preset templates included with the plugin and should be downloadable assets.)
}