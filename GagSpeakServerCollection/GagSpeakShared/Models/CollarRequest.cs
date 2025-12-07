using GagspeakAPI.Attributes;
using System.ComponentModel.DataAnnotations;

namespace GagspeakShared.Models;

/// <summary>
///     Sent requests expire after 8 hours (automatically rejected)
/// </summary>
public class CollarRequest
{
    [Key]
    [MaxLength(10)]
    public string UserUID { get; set; }
    public User User { get; set; }

    [Key]
    [MaxLength(10)]
    public string OtherUserUID { get; set; }
    public User OtherUser { get; set; }

    [Required]
    public DateTime CreationTime { get; set; } = DateTime.MinValue; // The time the request was created.
    public string InitialWriting { get; set; } = string.Empty;
    public CollarAccess OtherUserAccess { get; set; } = CollarAccess.None;
    public CollarAccess OwnerAccess { get; set; } = CollarAccess.None;
}
