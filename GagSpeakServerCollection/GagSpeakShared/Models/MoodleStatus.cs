using GagspeakAPI.Enums;
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
    public ICollection<LikesMoodles> LikesMoodles { get; set; } = new List<LikesMoodles>();
    // Navigation property for keywords
    public ICollection<MoodleKeyword> MoodleKeywords { get; set; } = new List<MoodleKeyword>();

    public int IconID { get; set; } = 0;
    public string Title { get; set; } = "UNK NAME";
    public string Description { get; set; } = string.Empty;
    public StatusType Type { get; set; } = StatusType.Positive;
    public bool Dispelable { get; set; } = true;
    public int Stacks { get; set; } = 1;
    public bool Persistent { get; set; } = false;
    public int Days { get; set; } = 0;
    public int Hours { get; set; } = 0;
    public int Minutes { get; set; } = 0;
    public int Seconds { get; set; } = 0;
    public bool NoExpire { get; set; } = false;
    public bool AsPermanent { get; set; } = false;
    public Guid StatusOnDispell { get; set; } = Guid.Empty;
    public string CustomVFXPath { get; set; } = string.Empty;
    public bool StackOnReapply { get; set; } = false;
    public int StacksIncOnReapply { get; set; } = 1;

    [NotMapped]
    public int LikeCount => LikesMoodles.Count;
}