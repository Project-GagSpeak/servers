using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

public class UserAchievementData
{
    [Required]
    [Key]
    [ForeignKey(nameof(User))]
    public string UserUID { get; set; }
    public User User { get; set; }

    /// <summary> The Base64 String of a clients Achievement SaveData in LightAchievement format.
    /// <para> If it needs a fresh generation, it will be set to null. </para>
    /// <para> If it contains any data at all, it will be non-null. </para>
    /// <para> If the string is non-null but unable to be properly generated, it should not allow save updates. </para>
    /// </summary>
    public string Base64AchievementData { get; set; }
}