using GagspeakAPI.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

/// <summary> The special child. </summary>
public class UserCollarData
{
    [Key]
    public string UserUID { get; set; }

    [ForeignKey(nameof(UserUID))]
    public virtual User User { get; set; }

    public bool Visuals { get; set; } = true;

    public byte Dye1 { get; set; } = 0;
    public byte Dye2 { get; set; } = 0;

    public Guid MoodleId { get; set; } = Guid.Empty;
    public int MoodleIconId { get; set; } = 0;
    public string MoodleTitle { get; set; } = string.Empty;
    public string MoodleDescription { get; set; } = string.Empty;
    public byte MoodleType { get; set; } = 0;
    public string MoodleVFXPath { get; set; } = string.Empty;

    public string Writing { get; set; } = string.Empty;

    public CollarAccess EditAccess { get; set; } = CollarAccess.None;
    public CollarAccess OwnerEditAccess { get; set; } = CollarAccess.None;

    public virtual ICollection<CollarOwner> Owners { get; set; } = new List<CollarOwner>();
}