using GagspeakAPI;
using GagspeakAPI.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

/// <summary>
///     Stores an uploaded pattern entry. Contains the base64 string data of the pattern.
///     Included in the table is components used for searching and categorizing.
/// </summary>
public class MoodleStatus
{
    [Key]
    public Guid Identifier { get; set; } // The GUID of the Moodle Status.

    [Required]
    public string PublisherUID { get; set; }
    public DateTime TimePublished { get; set; } // Time of Publication.

    public string Author { get; set; } // Alias Uploader Name
    public ICollection<LikesMoodles> LikesMoodles { get; set; } = new List<LikesMoodles>();
    public ICollection<MoodleKeyword> MoodleKeywords { get; set; } = new List<MoodleKeyword>();

    // Data parsed from MoodleStatusInfo
    public int Version { get; set; } = 0; // NEW (Current MyStatus.cs is version 2)
    public int IconID { get; set; } = 0; // NEW (Same as old)
    public string Title { get; set; } = "UNK NAME"; // NEW (Same as old)
    public string Description { get; set; } = string.Empty; // NEW (Same as old)
    public string CustomFXPath { get; set; } = string.Empty; // NEW (CustomVFXPath -> CustomFXPath)

    public StatusType Type { get; set; } = StatusType.Positive; // NEW (Same as old)
    public int Stacks { get; set; } = 1; // NEW (Same as old)
    public int StackSteps { get; set; } = 1; // NEW (Migrated [StacksIncOnReapply -> StackSteps])

    public Modifiers Modifiers { get; set; } = Modifiers.None; // NEW (Handles multiple old bools)
    public Guid ChainedStatus { get; set; } = Guid.Empty; // NEW (Migrated [StatusOnDispell -> ChainedStatus]) (EFMigrated!)
    public ChainTrigger ChainTrigger { get; set; } = ChainTrigger.Dispel; // NEW (Default to Dispel)

    // Should store expireTicks, but these can be generated from offset. Instead store D/H/M/S.
    public int Days { get; set; } = 0; // OLD
    public int Hours { get; set; } = 0; // OLD
    public int Minutes { get; set; } = 0; // OLD
    public int Seconds { get; set; } = 0; // OLD
    // Do not store applier or dispeller to mitigate risk of accidentally exposing someones identity.
    public bool Permanent { get; set; } = false; // NEW (Migrated [Persistent -> Permanent]) (EFMigrated!) *This is 'Sticky' in Moodles UI

    // OLD VALUES (For Migration Only)
    public bool Dispelable { get; set; } = true; // OLD (Migrate to Modifiers)
    public bool NoExpire { get; set; } = false; // OLD (Remove entirely)
    public bool AsPermanent { get; set; } = false;// OLD (Remove entirely)
    public bool StackOnReapply { get; set; } = false; // OLD (Migrate to Modifiers)

    [NotMapped]
    public int LikeCount => LikesMoodles.Count;
}