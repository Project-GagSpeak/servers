using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

/// <summary>
/// Stores an uploaded pattern entry. Contains the base64 string data of the pattern.
/// Included in the table is components used for searching and categorizing.
/// </summary>
public class PatternEntry
{
    [Key]
    public Guid Identifier { get; set; }

    [Required]
    public string PublisherUID { get; set; }
    public DateTime TimePublished { get; set; }

    [MaxLength(60)]
    public string Name { get; set; }

    [MaxLength(250)]
    public string Description { get; set; }
    public string Author { get; set; }
    public ICollection<PatternEntryTag> PatternEntryTags { get; set; } = new List<PatternEntryTag>();
    public int DownloadCount { get; set; } = 0;
    public ICollection<UserPatternLikes> UserPatternLikes { get; set; } = new List<UserPatternLikes>();
    public bool ShouldLoop { get; set; }
    public TimeSpan Length { get; set; }
    public bool UsesVibrations { get; set; }
    public bool UsesRotations { get; set; }
    public bool UsesOscillation { get; set; }
    public string Base64PatternData { get; set; }

    // Derived property to get the like count
    [NotMapped]
    public int LikeCount => UserPatternLikes.Count;
}