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

    /* ---------- Profile Data ---------- */
    public bool FlaggedForReport { get; set; } = false;
    public bool ProfileDisabled { get; set; } = false;
    public DateTime ProfileTimeoutTimeStamp { get; set; } // the time when the profile was timed out.
    public string Base64ProfilePic { get; set; } = string.Empty; // string.empty == no image provided.
    public string UserDescription { get; set; } = string.Empty;

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