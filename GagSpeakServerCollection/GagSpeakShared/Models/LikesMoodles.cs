using System.ComponentModel.DataAnnotations;

namespace GagspeakShared.Models;

/// <summary>
/// Tracks which MoodleStatus items a user has liked.
/// </summary>
public class LikesMoodles
{
    [Required]
    public string UserUID { get; set; }
    public User User { get; set; } = null!;

    [Required]
    public Guid MoodleStatusId { get; set; }
    public MoodleStatus MoodleStatus { get; set; } = null!;
}
