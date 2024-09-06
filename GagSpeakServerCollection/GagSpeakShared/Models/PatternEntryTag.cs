using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

/// <summary>
/// Associates the Stores relationships between patterns and tags.
/// Helpful for pattern looking when searching.
/// </summary>
public class PatternEntryTag
{
    [Key, Column(Order = 1)]
    [Required]  // Foreign key is required
    public Guid PatternEntryId { get; set; }

    [ForeignKey("PatternEntryId")]
    public PatternEntry PatternEntry { get; set; }

    [Key, Column(Order = 2)]
    [Required]  // Foreign key to the tag name is required
    [MaxLength(100)]  // Assuming tag names are at most 100 characters
    public string TagName { get; set; }

    [ForeignKey("TagName")]
    public PatternTag Tag { get; set; } // can have many
}
