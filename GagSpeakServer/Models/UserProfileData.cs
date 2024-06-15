using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakServer.Models;

public class UserProfileData
{
     public string Base64ProfilePic { get; set; } // the base64 string of the user's profile picture
     public bool FlaggedForReport { get; set; }   // is the profile flagged for report?  
     public bool ProfileDisabled { get; set; }    // if the profile is disabled
     public User User { get; set; }               // the user profile this profiledata is for         
     public string UserDescription { get; set; }  // the user's description

     // the UserUID is a foreign key to the User table
     // this means that the UserUID must be a primary key in the User table,
     // and also that the UserUID must be unique in the User table
     // (This is also the primary key for this class)
     [Required]
     [Key]
     [ForeignKey(nameof(User))]
     public string UserUID { get; set; }
}