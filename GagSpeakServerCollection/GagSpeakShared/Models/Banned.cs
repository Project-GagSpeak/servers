using System.ComponentModel.DataAnnotations;

namespace GagspeakShared.Models;

/// <summary> I really didnt want to add this class, but i guess there are going to 
/// be some bad apples that will try to ruin this expreience for everyone, wont there  </summary>
public class Banned
{
    [Key]
    [MaxLength(100)]
    public string CharacterIdentification { get; set; } // indent (identifier) of the character that was banned.
    public string Reason { get; set; } // why they were banned
    [Timestamp]
    public byte[] Timestamp { get; set; } // when they were banned
}
