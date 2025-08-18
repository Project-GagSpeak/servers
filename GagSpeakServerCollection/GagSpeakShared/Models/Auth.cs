using System.ComponentModel.DataAnnotations;

namespace GagspeakShared.Models;
#pragma warning disable CS8632

/// <summary>
///     The <b>Auth</b> class represents a user's authentication information. <para />
///     The <b>HashedKey</b> property is a unique identifier, and all auth models 
///     store the primary user UID so we know who the account owner is if it is a secondary account.
/// </summary>
public class Auth
{
     [Key]
     [MaxLength(64)]
     public string HashedKey { get; set; }        // The "Secret Key" for a profile. The secret key where the UserUID == PrimaryUserUID is the account secret key.

     public string UserUID { get; set; }
     public User User { get; set; }
     public bool IsBanned { get; set; }
    public string? PrimaryUserUID { get; set; }  // the UID of the first profile made under this account
    public User? PrimaryUser { get; set; }       // the user profile object of the first profile made under this account
}
#pragma warning restore CS8632