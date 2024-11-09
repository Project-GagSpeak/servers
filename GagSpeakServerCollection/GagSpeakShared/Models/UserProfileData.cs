using GagspeakAPI.Data.IPC;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

public class UserProfileData
{
    // the UserUID is a foreign key to the User table
    // this means that the UserUID must be a primary key in the User table,
    // and also that the UserUID must be unique in the User table
    // (This is also the primary key for this class)
    [Required]
    [Key]
    [ForeignKey(nameof(User))]
    public string UserUID { get; set; }
    public User User { get; set; }

    /// <summary>
    /// Defines if a profile is displayed to any user that is not a direct pair of them.
    /// Keeping this false will hide the profile picture and description from any unpaired users.
    /// </summary>
    public bool ProfileIsPublic { get; set; } = false;

    /// <summary>
    /// Determines if a user is currently flagged for report.
    /// Flagged Kinksters will still be able to see their own profile, however, no one else will.
    /// This is done intentionally to prevent predators from watching when a profile is reported to point the blame on someone.
    /// </summary>
    public bool FlaggedForReport { get; set; } = false;

    /// <summary>
    /// Determines if a profile has been disabled.
    /// Disabled Profiles can still show decoration and titles, but the images and descriptions will no longer be modifiable.
    /// </summary>
    public bool ProfileDisabled { get; set; } = false;

    /// <summary>
    /// The number of warnings that this profile has recieved.
    /// Profile warnings go up by one whenever a profile is cleared, or whenever a malicious report attempt is made.
    /// Warning strikes are a simple way to show the CK Team how many repeat offenses a profile has had when we evalulate reports.
    /// </summary>
    public int WarningStrikeCount { get; set; } = 0;

    public string Base64ProfilePic { get; set; } = string.Empty; // string.empty == no image provided.
    public string UserDescription { get; set; } = string.Empty; // Description of the user.
    public int CompletedAchievementsTotal { get; set; } = 0; // Total number of achievements completed.
    public int ChosenTitleId { get; set; } = 0; // Chosen Achievement Title. 0 == no title chosen.

    public ProfileStyleBG PlateBackground { get; set; } = ProfileStyleBG.Default;
    public ProfileStyleBorder PlateBorder { get; set; } = ProfileStyleBorder.Default;

    public ProfileStyleBorder ProfilePictureBorder { get; set; } = ProfileStyleBorder.Default;
    public ProfileStyleOverlay ProfilePictureOverlay { get; set; } = ProfileStyleOverlay.Default;

    public ProfileStyleBG DescriptionBackground { get; set; } = ProfileStyleBG.Default;
    public ProfileStyleBorder DescriptionBorder { get; set; } = ProfileStyleBorder.Default;
    public ProfileStyleOverlay DescriptionOverlay { get; set; } = ProfileStyleOverlay.Default;

    public ProfileStyleBG GagSlotBackground { get; set; } = ProfileStyleBG.Default;
    public ProfileStyleBorder GagSlotBorder { get; set; } = ProfileStyleBorder.Default;
    public ProfileStyleOverlay GagSlotOverlay { get; set; } = ProfileStyleOverlay.Default;

    public ProfileStyleBG PadlockBackground { get; set; } = ProfileStyleBG.Default;
    public ProfileStyleBorder PadlockBorder { get; set; } = ProfileStyleBorder.Default;
    public ProfileStyleOverlay PadlockOverlay { get; set; } = ProfileStyleOverlay.Default;

    public ProfileStyleBG BlockedSlotsBackground { get; set; } = ProfileStyleBG.Default;
    public ProfileStyleBorder BlockedSlotsBorder { get; set; } = ProfileStyleBorder.Default;
    public ProfileStyleOverlay BlockedSlotsOverlay { get; set; } = ProfileStyleOverlay.Default;

    public ProfileStyleBorder BlockedSlotBorder { get; set; } = ProfileStyleBorder.Default;
    public ProfileStyleOverlay BlockedSlotOverlay { get; set; } = ProfileStyleOverlay.Default;
}