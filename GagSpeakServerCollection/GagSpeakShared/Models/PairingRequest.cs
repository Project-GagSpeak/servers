using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

/// <summary>
///   Defines a request sent from <b>User</b> to <b>OtherUser</b> <para />
///   Sent requests expire after 3 days, (and are automatically rejected).
/// </summary>
public class PairingRequest
{
    [Key, Column(Order = 0)] // Request Sender
    [MaxLength(10)]
    public string UserUID { get; set; }         // The UserUID that sent the request to add the other user
    public User User { get; set; }              // The User object of this kinkster.

    [Key, Column(Order = 1)]
    [MaxLength(10)]
    public string OtherUserUID { get; set; }    // The UserUID that will be added to UserUID's list when accepted.
    public User OtherUser { get; set; }         // The User object of the other user

    [Required]
    public DateTime CreationTime { get; set; } = DateTime.MinValue;

    // If the request is for a temporary pairing, or a permanent one.
    public bool IsTemporary { get; set; } = false;

    // Optionally attached message.
    public string AttachedMessage { get; set; } = string.Empty;
}
