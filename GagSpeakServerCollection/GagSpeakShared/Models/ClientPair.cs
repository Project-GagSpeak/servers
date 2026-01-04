using System.ComponentModel.DataAnnotations;

namespace GagspeakShared.Models;

/// <summary>
/// The <c>ClientPair</c> class represents USER A pairing with USER B. This is NOT bidirectional.
/// <para> Primary key is a composite key of UserUID and OtherUserUID. Allows for USER A to USER B and for USER B to USER A </para>
/// <para> Both UserUID and OtherUserUID are indexed for fasterlook </para>
/// </summary>
public class ClientPair
{
    [Key]
    [MaxLength(10)] // Composite key with OtherUserUID
    public string UserUID { get; set; }          // the UID of client's user
    public User User { get; set; }               // the user object of the client's user

    [Key]
    [MaxLength(10)] // Composite key with UserUID
    public string OtherUserUID { get; set; }     // the UID of the other user
    public User OtherUser { get; set; }          // the user object of the other user

    [Timestamp] // replace with time of commitment later
    public byte[] Timestamp { get; set; }   // timestamp when the pair was created

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // when the pair was created
    public string TempAccepterUID { get; set; } = string.Empty;
}
