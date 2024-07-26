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

    // if they are currently in the room.
    // If a user leaves a room, this is set to false.
    // if they remove the room, the full row of this in the table is removed.
    public bool InRoom { get; set; }

    // If true, pair is added to a group for the context of the hub.
    public bool AllowingVibe { get; set; }
}
