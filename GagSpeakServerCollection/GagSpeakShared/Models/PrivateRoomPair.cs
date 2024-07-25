using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;
using GagspeakAPI.Data.Enum;

namespace GagspeakShared.Models;

/// <summary> Represents a user profile in the system. (will only ever link to one playercharacter)</summary>
public class PrivateRoomPair
{
    // the room we are referencing
    [Required]
    [Key]
    [ForeignKey(nameof(PrivateRoom))]
    public string PrivateRoomNameID { get; set; }
    public PrivateRoom PrivateRoom { get; set; }

    // who is in the room
    [Required]
    [Key]
    [ForeignKey(nameof(PrivateRoomUser))]
    public string PrivateRoomUserUID { get; set; }
    public User PrivateRoomUser { get; set; }

    // our chat alias name
    public string ChatAlias { get; set; }
}
