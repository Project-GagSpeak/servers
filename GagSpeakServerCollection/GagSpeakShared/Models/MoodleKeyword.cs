using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

/// <summary>
/// Associates the Stores relationships between patterns and keywords
/// </summary>
public class MoodleKeyword
{
    [Key]
    [Required]  // Foreign key is required
    public Guid MoodleStatusId { get; set; }
    [ForeignKey(nameof(MoodleStatusId))]
    public MoodleStatus MoodleStatus { get; set; } = null!;

    [Required]
    [MaxLength(30)]
    public string KeywordWord { get; set; } = string.Empty; // Foreign Key for Keyword
    [ForeignKey(nameof(KeywordWord))]
    public Keyword Keyword { get; set; } = null!;
}
