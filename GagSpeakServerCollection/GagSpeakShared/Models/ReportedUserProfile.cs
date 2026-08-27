using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

/// <summary>
///   Stores information for moderation review on reports regarding a users profile.
/// </summary>
public class ReportedUserProfile
{
    // create a generated key on initialization
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ReportID { get; set; }

    // Some Required Information to be in a report.
    [Required] public DateTime ReportTime { get; set; }
    [Required] public bool SnapshotIsNSFW { get; set; }
    [Required] public string SnapshotIcon { get; set; }
    [Required] public string SnapshotBg { get; set; }
    [Required] public string SnapshotJson { get; set; }

    // Player who made the report.
    [ForeignKey(nameof(ReportingUser))]
    public string ReportingUserUID { get; set; }
    public User ReportingUser { get; set; }

    // Player being reported.
    [ForeignKey(nameof(ReportedUser))]
    public string ReportedUserUID { get; set; }
    public User ReportedUser { get; set; }

    // Store the reason for the report.
    public string ReportReason { get; set; } = string.Empty;
}