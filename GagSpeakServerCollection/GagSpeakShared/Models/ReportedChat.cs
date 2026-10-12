using GagspeakAPI.Chat;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

/// <summary>
///   Stores reports for global chat misconduct.
/// </summary>
public class ReportedChat
{
    [Key] // Ensure report uniqueness.
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ReportID { get; set; }

    // Some Required Information to be in a report.
    [Required] public GsChatKind Chat { get; set; } = GsChatKind.Global;
    [Required] public DateTime ReportTime { get; set; }

    public string ChatLogId { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ChatContextJson { get; set; } = string.Empty;

    public string ReportedUserUID { get; set; } = string.Empty;

    // Reporter.
    public string ReporterUID { get; set; }
    [ForeignKey(nameof(ReporterUID))]
    public User Reporter { get; set; }

    // Reason for report.
    public string ReportReason { get; set; } = string.Empty;
}