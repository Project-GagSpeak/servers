using GagspeakAPI.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GagspeakShared.Models;

public class UserProfileData
{
    [Key]
    public string UserUID { get; set; }
    
    [ForeignKey(nameof(UserUID))]
    public virtual User User { get; set; }

    // reference to the user's collar data so that we can obtain its writing and owner UID's.
    [ForeignKey(nameof(UserUID))]
    public virtual UserCollarData CollarData { get; set; }

    /// <summary>
    ///     Defines if a profile is displayed to any user that is not a direct pair of them. <para />
    ///     Keeping this false will hide the profile picture and description from any unpaired users.
    /// </summary>
    public bool ProfileIsPublic { get; set; } = false;

    /// <summary>
    ///     Determines if a user is currently flagged for report. <para />
    ///     Others cannot see flagged Kinkster's profiles, but they will be able to. <para />
    ///     This is done intentionally to prevent predators from watching when a 
    ///     profile is reported to point the blame on someone.
    /// </summary>
    public bool FlaggedForReport { get; set; } = false;

    public string Base64ProfilePic      { get; set; } = string.Empty;   // empty is no image provided.
    public string Description           { get; set; } = string.Empty;   // Description of the user.
    public int    AchievementsEarned    { get; set; } = 0;              // Total number of achievements completed.
    public int    ChosenTitleId         { get; set; } = 0;              // Chosen Achievement Title. 0 == no title chosen.

    // S - T - Y - L - E
    public KinkPlateBG      PlateBG             { get; set; } = KinkPlateBG.Default;
    public KinkPlateBorder  PlateBorder         { get; set; } = KinkPlateBorder.Default;
    
    public KinkPlateBG      PlateLightBG        { get; set; } = KinkPlateBG.Default;
    public KinkPlateBorder  PlateLightBorder    { get; set; } = KinkPlateBorder.Default;

    public KinkPlateBorder  AvatarBorder        { get; set; } = KinkPlateBorder.Default;
    public KinkPlateOverlay AvatarOverlay       { get; set; } = KinkPlateOverlay.Default;

    public KinkPlateBG      DescriptionBG       { get; set; } = KinkPlateBG.Default;
    public KinkPlateBorder  DescriptionBorder   { get; set; } = KinkPlateBorder.Default;
    public KinkPlateOverlay DescriptionOverlay  { get; set; } = KinkPlateOverlay.Default;

    public KinkPlateBG      GagSlotBG           { get; set; } = KinkPlateBG.Default;
    public KinkPlateBorder  GagSlotBorder       { get; set; } = KinkPlateBorder.Default;
    public KinkPlateOverlay GagSlotOverlay      { get; set; } = KinkPlateOverlay.Default;

    public KinkPlateBG      PadlockBG           { get; set; } = KinkPlateBG.Default;
    public KinkPlateBorder  PadlockBorder       { get; set; } = KinkPlateBorder.Default;
    public KinkPlateOverlay PadlockOverlay      { get; set; } = KinkPlateOverlay.Default;

    public KinkPlateBG      BlockedSlotsBG      { get; set; } = KinkPlateBG.Default;
    public KinkPlateBorder  BlockedSlotsBorder  { get; set; } = KinkPlateBorder.Default;
    public KinkPlateOverlay BlockedSlotsOverlay { get; set; } = KinkPlateOverlay.Default;

    public KinkPlateBorder  BlockedSlotBorder   { get; set; } = KinkPlateBorder.Default;
    public KinkPlateOverlay BlockedSlotOverlay  { get; set; } = KinkPlateOverlay.Default;
}