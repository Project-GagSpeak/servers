using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

public class Keyword
{
    [Key]
    public string Word { get; set; }

    // Relationships
    public ICollection<MoodleKeyword> MoodleKeywords { get; set; } = new List<MoodleKeyword>();
    public ICollection<PatternKeyword> PatternKeywords { get; set; } = new List<PatternKeyword>();
}