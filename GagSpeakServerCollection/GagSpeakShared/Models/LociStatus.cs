using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

/// <summary>
///     Stores an uploaded Loci Status entry
/// </summary>
public class LociStatus
{
    [Key]
    public Guid Identifier { get; set; } // The GUID of the Loci Status.

    [Required]
    public string PublisherUID { get; set; }
    public DateTime TimePublished { get; set; } // Time of Publication.

    public string Author { get; set; } // Alias Uploader Name
    public ICollection<LikesLoci> LikesLoci { get; set; } = new List<LikesLoci>();
    public ICollection<LociKeyword> LociKeywords { get; set; } = new List<LociKeyword>();

    // Data parsed from MoodleStatusInfo
    public int Version { get; set; } = 2;
    public uint IconID { get; set; } = 0;
    public string Title { get; set; } = "UNK NAME";
    public string Description { get; set; } = string.Empty;
    public string CustomFXPath { get; set; } = string.Empty;

    public byte Type { get; set; } = 0;
    public int Stacks { get; set; } = 1;
    public int StackSteps { get; set; } = 1;
    public int StackToChain { get; set; } = 0; // NEW for v3

    public uint Modifiers { get; set; } = 0;
    public Guid ChainedGUID { get; set; } = Guid.Empty;
    public byte ChainType { get; set; } = 0; // NEW for v3
    public int ChainTrigger { get; set; } = 0;

    // Should store expireTicks, but these can be generated from offset. Instead store D/H/M/S.
    public int Days { get; set; } = 0;
    public int Hours { get; set; } = 0;
    public int Minutes { get; set; } = 0;
    public int Seconds { get; set; } = 0;

    // Do not store applier or dispeller to mitigate risk of accidentally exposing someones identity.
    public bool Permanent { get; set; } = false;

    [NotMapped]
    public int LikeCount => LikesLoci.Count;
}