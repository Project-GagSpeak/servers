using System.ComponentModel.DataAnnotations;

namespace GagspeakServer.Models;

/// <summary>
/// The <c>Auth</c> Auth class represents a user's authentication information.
/// <para> The <c>HashedKey</c> property is a unique identifier, and all auth models 
/// store the primary user UID so we know who the account owner is if it is a secondary account. </para>
/// </summary>
public class Auth
{
     [Key]
     [MaxLength(64)]
     public string HashedKey { get; set; }        // The "Secret Key" for a profile. The secret key where the UserUID == PrimaryUserUID is the account secret key.

     public string UserUID { get; set; }          // the UID of this profile
     public User User { get; set; }               // the playercharacter user
     public bool IsBanned { get; set; }           // if this profile is banned or not
     public string? PrimaryUserUID { get; set; }  // the UID of the first profile made under this account
     public User? PrimaryUser { get; set; }       // the user profile object of the first profile made under this account
}
