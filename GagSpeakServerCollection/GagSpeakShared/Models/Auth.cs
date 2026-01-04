using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;
#pragma warning disable CS8632

/// <summary>
///     The <b>Auth</b> class represents a user's authentication information. <para />
///     The <b>HashedKey</b> property is a unique identifier, and all auth models 
///     store the primary user UID so we know who the account owner is if it is a secondary account.
/// </summary>
public class Auth
{
    // The "Secret Key" for a profile.
    [Key]
    [MaxLength(64)]
    public string HashedKey { get; set; }

    [Required]
    public string UserUID { get; set; }

    [ForeignKey(nameof(UserUID))]
    public virtual User User { get; set; }

    [Required]
    public string PrimaryUserUID { get; set; }

    [ForeignKey(nameof(PrimaryUserUID))]
    public virtual User PrimaryUser { get; set; }

    [ForeignKey(nameof(PrimaryUserUID))]
    public virtual AccountReputation AccountRep { get; set; }

    [NotMapped] public bool IsPrimary => string.Equals(UserUID, PrimaryUserUID);

    // Designed for efficient loading. Without any includes, this will only retrieve the HashedKey, UserUID, and PrimaryUID.
    // If we need to scan for account validation, we can make use of accessing the account reputation.
}
#pragma warning restore CS8632