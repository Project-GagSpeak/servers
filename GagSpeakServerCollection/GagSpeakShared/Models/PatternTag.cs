using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

public class PatternTag
{
    [Key]
    public string Name { get; set; }

    public ICollection<PatternEntryTag> PatternEntryTags { get; set; }
}