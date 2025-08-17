using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

/// <summary>
///     Represents an owner of a user's collar.
/// </summary>
public class CollarOwner
{
    [Key]
    [Column(Order = 0)]
    [MaxLength(10)]
    public string OwnerUID { get; set; }

    [ForeignKey(nameof(OwnerUID))]
    public User Owner { get; set; }

    [Key]
    [Column(Order = 1)]
    [MaxLength(10)]
    public string CollaredUserUID { get; set; } // The collared user's user ID

    [ForeignKey(nameof(CollaredUserUID))]
    public virtual UserCollarData CollaredUserData { get; set; }
}