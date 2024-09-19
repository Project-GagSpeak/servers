using System.ComponentModel.DataAnnotations;

namespace GagspeakShared.Models;

/// <summary> Represents a user profile in the system. (will only ever link to one playercharacter)</summary>
public class PrivateRoom
{
    [Key]
    [MaxLength(50)]
    public string NameID { get; set; } // The name of the room.

    // by not making the host a key or reference, but simply an index, we allow hosts to be changable
    public string HostUID { get; set; }
    public User Host { get; set; }

    // the time the room was made (Clean Rooms made past 12 hours of creation)
    public DateTime TimeMade { get; set; }
}
