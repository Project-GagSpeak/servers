using System.ComponentModel.DataAnnotations;

namespace GagspeakShared.Models;

/// <summary> I really didnt want to add this class, but i guess there are going to
/// be some bad apples that will try to ruin this expreience for everyone, wont there  </summary>
public class BannedRegistrations
{
    [Key]
    [MaxLength(100)]
    public string DiscordId { get; set; } // ID of the discord user who was banned, preventing them from using the discord service.
}
