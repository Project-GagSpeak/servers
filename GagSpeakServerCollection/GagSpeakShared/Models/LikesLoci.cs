using System.ComponentModel.DataAnnotations;

namespace GagspeakShared.Models;

/// <summary>
///   Tracks the like status for a published LociStatus. <para />
///   Realistically, this is a terrible way to do it and we should migrate to redis. <br/>
///   Only do this migration when we know comfortably how to keep it sustained.
/// </summary>
public class LikesLoci
{
    [Required]
    public string UserUID { get; set; }
    public User User { get; set; } = null!;

    [Required]
    public Guid LociStatusId { get; set; }
    public LociStatus LociStatus { get; set; } = null!;
}
