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
    public Guid ActiveSetId { get; set; } = Guid.Empty; // the ID of the user's active outfit
    public string ActiveSetEnabler { get; set; } = ""; // person who Enabled the set.
    public string ActiveSetPadLock { get; set; } = Padlocks.None.ToName(); // Type of padlock used to lock the set.
	public string ActiveSetPassword { get; set; } = ""; // password bound to the set's lock type.
	public DateTimeOffset ActiveSetLockTime { get; set; } = DateTimeOffset.UtcNow; // timer placed on the set's lock
	public string ActiveSetLockAssigner { get; set; } = ""; // UID that locked the set.
}