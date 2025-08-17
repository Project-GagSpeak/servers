using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

// Maintainers note: this is a better way of handling tracking for relations without bloating the DB.

// It should be possible to also do this for each user's likes, to store a lazy loaded collection of
// liked patterns and moodles per user, and have the pattern like count be a static int in sync.
// this will lose the mutability but save a lot of space, so do this if we are in a storage crisis.
public class Keyword
{
    [Key]
    public string Word { get; set; }

    // Relationships
    public ICollection<MoodleKeyword> MoodleKeywords { get; set; } = new List<MoodleKeyword>();
    public ICollection<PatternKeyword> PatternKeywords { get; set; } = new List<PatternKeyword>();
}