using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

/// <summary>
/// Serves as a server-side only table for help with referencing 
/// if updated data for another pair is valid or not.
/// <para>
/// Because when we update other pair info we cannot pass in their previous info, 
/// its best to have a logged state of their current info present in the DB.
/// </para>
/// <para><b>
/// IMPORTANT: THIS CAN HELP SERVE AS A WAY TO PREVENT CONCURRENT UPDATES TO THE SAME DATA.
/// </b></para>
/// </summary>
public class UserActiveStateData
{
    [Required]
    [Key]
    [ForeignKey(nameof(User))]
    public string UserUID { get; set; }
    public User User { get; set; }

    /* User's WardrobeData state references */
    public string WardrobeActiveSetName { get; set; } = ""; // the name of the user's active outfit
    public string WardrobeActiveSetAssigner { get; set; } = ""; // person who Enabled the set.
    public bool   WardrobeActiveSetLocked { get; set; } = false; // the lock status of the user's active outfit
    public string WardrobeActiveSetLockAssigner { get; set; } = ""; // person who Locked the set.
    public DateTimeOffset WardrobeActiveSetLockTime { get; set; } = DateTimeOffset.MinValue; // when the locked set will expire

    /* User's ToyboxData state references */
    public string ToyboxActivePatternName { get; set; } = ""; // the name of the user's actively running pattern

    /* User's MoodlesState Data TODO: Implement this */
}