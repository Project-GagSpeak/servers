using System.ComponentModel.DataAnnotations;

namespace GagspeakShared.Models;

/// <summary> Keeps track of the patterns a User has liked. </summary>
public class UserPatternLikes
{
    [Required]
    public string UserUID { get; set; }
    public User User { get; set; }

    [Required]
    public Guid PatternEntryId { get; set; }
    public PatternEntry PatternEntry { get; set; }
}
