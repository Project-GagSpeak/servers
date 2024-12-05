using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

/// <summary>
/// Stores an uploaded pattern entry. Contains the base64 string data of the pattern.
/// Included in the table is components used for searching and categorizing.
/// </summary>
public class MoodleStatus
{
    [Key]
    public Guid Identifier { get; set; }

    [Required]
    public string PublisherUID { get; set; }
    public DateTime TimePublished { get; set; } // Time of Publication.

    public string Author { get; set; } // Alias Uploader Name
    public ICollection<LikesPatterns> UserPatternLikes { get; set; } = new List<LikesPatterns>();
    // Navigation property for keywords
    public ICollection<MoodleKeyword> MoodleKeywords { get; set; } = new List<MoodleKeyword>();
}