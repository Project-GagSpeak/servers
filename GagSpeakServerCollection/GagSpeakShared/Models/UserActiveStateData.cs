using GagspeakAPI.Enums;
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
    public string WardrobeActiveSetPadLock { get; set; } = Padlocks.None.ToName(); // Type of padlock used to lock the set.
	public string WardrobeActiveSetPassword { get; set; } = ""; // password bound to the set's lock type.
	public DateTimeOffset WardrobeActiveSetLockTime { get; set; } = DateTimeOffset.UtcNow; // timer placed on the set's lock
	public string WardrobeActiveSetLockAssigner { get; set; } = ""; // UID that locked the set.

    /* User's ToyboxData state references */
    public Guid ToyboxActivePatternId { get; set; } = Guid.Empty; // the name of the user's actively running pattern
}