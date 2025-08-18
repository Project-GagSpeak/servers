using GagspeakAPI.Data;
using GagspeakShared.Models;
using System.Reflection;

namespace GagspeakServer;

public static class Extensions
{
    public static MoodlesStatusInfo ToStatusInfo(this MoodleStatus status)
        => new MoodlesStatusInfo()
        {
            GUID = status.Identifier,
            IconID = status.IconID,
            Title = status.Title,
            Description = status.Description,
            Type = status.Type,
            Applier = string.Empty,
            Dispelable = status.Dispelable,
            Stacks = status.Stacks,
            Persistent = status.Persistent,
            Days = status.Days,
            Hours = status.Hours,
            Minutes = status.Minutes,
            Seconds = status.Seconds,
            NoExpire = status.NoExpire,
            AsPermanent = status.AsPermanent,
            StatusOnDispell = status.StatusOnDispell,
            CustomVFXPath = status.CustomVFXPath,
            StackOnReapply = status.StackOnReapply,
            StacksIncOnReapply = status.StacksIncOnReapply
        };

    public static PublishedMoodle ToPublishedMoodle(this MoodleStatus status)
        => new PublishedMoodle() { AuthorName = status.Author, MoodleStatus = status.ToStatusInfo() };

    public static PublishedPattern ToPublishedPattern(this PatternEntry pattern)
        => new PublishedPattern()
        {
            Version = pattern.Version,
            Identifier = pattern.Identifier,
            Label = pattern.Name,
            Description = pattern.Description,
            Author = pattern.Author,
            Looping = pattern.ShouldLoop,
            Length = pattern.Length,
            UploadedDate = pattern.TimePublished,
            PrimaryDevice = pattern.PrimaryDeviceUsed,
            SecondaryDevice = pattern.SecondaryDeviceUsed,
            MotorsUsed = pattern.MotorsUsed
        };

    public static void UpdateInfoFromDto(this UserProfileData storedData, KinkPlateContent dtoContent)
    {
        // update all other values from the Info in the dto.
        storedData.ProfileIsPublic = dtoContent.PublicPlate;
        storedData.UserDescription = dtoContent.Description;
        storedData.CompletedAchievementsTotal = dtoContent.CompletedAchievementsTotal;
        storedData.ChosenTitleId = dtoContent.ChosenTitleId;

        storedData.PlateBackground = dtoContent.PlateBackground;
        storedData.PlateBorder = dtoContent.PlateBorder;

        storedData.ProfilePictureBorder = dtoContent.ProfilePictureBorder;
        storedData.ProfilePictureOverlay = dtoContent.ProfilePictureOverlay;

        storedData.DescriptionBackground = dtoContent.DescriptionBackground;
        storedData.DescriptionBorder = dtoContent.DescriptionBorder;
        storedData.DescriptionOverlay = dtoContent.DescriptionOverlay;

        storedData.GagSlotBackground = dtoContent.GagSlotBackground;
        storedData.GagSlotBorder = dtoContent.GagSlotBorder;
        storedData.GagSlotOverlay = dtoContent.GagSlotOverlay;

        storedData.PadlockBackground = dtoContent.PadlockBackground;
        storedData.PadlockBorder = dtoContent.PadlockBorder;
        storedData.PadlockOverlay = dtoContent.PadlockOverlay;

        storedData.BlockedSlotsBackground = dtoContent.BlockedSlotsBackground;
        storedData.BlockedSlotsBorder = dtoContent.BlockedSlotsBorder;
        storedData.BlockedSlotsOverlay = dtoContent.BlockedSlotsOverlay;

        storedData.BlockedSlotBorder = dtoContent.BlockedSlotBorder;
        storedData.BlockedSlotOverlay = dtoContent.BlockedSlotOverlay;
    }

    public static KinkPlateContent FromProfileData(this UserProfileData data)
        => new KinkPlateContent()
        {
            PublicPlate = data.ProfileIsPublic,
            Flagged = data.FlaggedForReport,
            Disabled = data.ProfileDisabled,
            Description = data.UserDescription,
            CompletedAchievementsTotal = data.CompletedAchievementsTotal,
            ChosenTitleId = data.ChosenTitleId,

            PlateBackground = data.PlateBackground,
            PlateBorder = data.PlateBorder,

            ProfilePictureBorder = data.ProfilePictureBorder,
            ProfilePictureOverlay = data.ProfilePictureOverlay,

            DescriptionBackground = data.DescriptionBackground,
            DescriptionBorder = data.DescriptionBorder,
            DescriptionOverlay = data.DescriptionOverlay,

            GagSlotBackground = data.GagSlotBackground,
            GagSlotBorder = data.GagSlotBorder,
            GagSlotOverlay = data.GagSlotOverlay,

            PadlockBackground = data.PadlockBackground,
            PadlockBorder = data.PadlockBorder,
            PadlockOverlay = data.PadlockOverlay,

            BlockedSlotsBackground = data.BlockedSlotsBackground,
            BlockedSlotsBorder = data.BlockedSlotsBorder,
            BlockedSlotsOverlay = data.BlockedSlotsOverlay,

            BlockedSlotBorder = data.BlockedSlotBorder,
            BlockedSlotOverlay = data.BlockedSlotOverlay
        };

    public static UserData ToUserData(this User user)
        => new UserData(user.UID, user.Verified, user.Alias, user.VanityTier, user.CreatedDate);

    /// <summary>
    ///     Copies all properties over from two objects of the same <typeparamref name="T"/> type. <para />
    ///     This honestly might be better an an API method lol.
    /// </summary>
    public static void CopyPropertiesTo<T>(this T source, T target)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source), "Source object is null");
        if (target is null)
            throw new ArgumentNullException(nameof(target), "Target object is null");


        Type type = typeof(T);
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (PropertyInfo property in properties)
        {
            if (property.CanRead && property.CanWrite)
            {
                object value = property.GetValue(source, null);
                property.SetValue(target, value, null);
            }
        }
    }
}