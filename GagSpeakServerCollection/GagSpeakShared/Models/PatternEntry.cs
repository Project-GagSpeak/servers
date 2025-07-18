using GagspeakAPI.Attributes;
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
    public ICollection<PatternKeyword> PatternKeywords { get; set; } = new List<PatternKeyword>();
    public int DownloadCount { get; set; } = 0;
    public ICollection<LikesPatterns> UserPatternLikes { get; set; } = new List<LikesPatterns>();
    public bool ShouldLoop { get; set; }
    public TimeSpan Length { get; set; }
    public ToyBrandName PrimaryDeviceUsed { get; set; } = ToyBrandName.Unknown;
    public ToyBrandName SecondaryDeviceUsed { get; set; } = ToyBrandName.Unknown;
    public ToyMotor MotorsUsed { get; set; } = ToyMotor.Unknown;
    public string Base64PatternData { get; set; }

    // Versioning to detect update changes in uploaded pattern data.
    public int Version { get; set; } = 2;

    // Derived property to get the like count
    [NotMapped]
    public int LikeCount => UserPatternLikes.Count;
}