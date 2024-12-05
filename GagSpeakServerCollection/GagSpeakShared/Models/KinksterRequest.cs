using System.ComponentModel.DataAnnotations;

namespace GagspeakShared.Models;

/// <summary>
/// The KinksterRequest Model stores the Userdata of the user who send the request
/// and the use who received the request.
/// <para>
/// All sent requests expire after 3 days, (and are automatically rejected).
/// </para>
/// </summary>
public class KinksterRequest
{
    [Key]
    [MaxLength(10)] // Composite key with OtherUserUID
    public string UserUID { get; set; }         // The UserUID that sent the request to add the other user
    public User User { get; set; }              // The User object of this kinkster.

    [Key]
    [MaxLength(10)] // Composite key with UserUID
    public string OtherUserUID { get; set; }    // The UserUID that will be added to UserUID's list when accepted.
    public User OtherUser { get; set; }         // The User object of the other user

    [Required]
    public DateTime CreationTime { get; set; }   // timestamp when the pair was created
}
