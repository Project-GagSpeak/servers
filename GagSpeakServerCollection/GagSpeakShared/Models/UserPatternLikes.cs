using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GagspeakAPI.Data.Enum;

namespace GagspeakShared.Models;

/// <summary> Keeps track of the patterns a User has liked. </summary>
public class UserPatternLikes
{
    [Required]
    [Key]
    [ForeignKey(nameof(User))]
    public string UserUID { get; set; }
    public User User { get; set; }

    [Required]
    [Key]
    [ForeignKey(nameof(PatternEntry))]
    public Guid PatternEntryId { get; set; }
    public PatternEntry PatternEntry { get; set; }
}
