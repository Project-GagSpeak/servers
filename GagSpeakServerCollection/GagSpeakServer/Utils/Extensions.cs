using GagspeakAPI.Data;
using GagspeakAPI.Network;
using GagspeakAPI.User;
using GagspeakShared.Models;
using System.Reflection;

namespace GagspeakServer;

public static class Extensions
{
    public static LociStatusStruct ToStruct(this LociStatus status)
    {
        var duration = new TimeSpan(status.Days, status.Hours, status.Minutes, status.Seconds);
        long expireTicks = status.Permanent ? -1 : (long)duration.TotalMilliseconds;
        return new LociStatusStruct()
        {
            Version = status.Version,
            GUID = status.Identifier,
            IconID = status.IconID,
            Title = status.Title,
            Description = status.Description,
            CustomVFXPath = status.CustomFXPath,
            ExpireTicks = expireTicks,
            Type = status.Type,
            Stacks = status.Stacks,
            StackSteps = status.StackSteps,
            StackToChain = status.StackToChain,
            Modifiers = status.Modifiers,
            ChainedGUID = status.ChainedGUID,
            ChainType = status.ChainType,
            ChainTrigger = status.ChainTrigger,
            Applier = string.Empty,
            Dispeller = string.Empty,
        };
    }

    public static PublishedLociData ToPublishedMoodle(this LociStatus status)
        => new PublishedLociData(status.Author, status.ToStruct());

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

    public static void UpdateInfoFromDto(this UserProfile storedData, KinkPlateContent dtoContent)
    {
        // update all other values from the Info in the dto.
        storedData.Description = dtoContent.Description;
        storedData.AchievementsEarned = dtoContent.CompletedTotal;
        storedData.ChosenTitleId = dtoContent.ChosenTitleId;

        storedData.PlateBG = dtoContent.PlateBG;
        storedData.PlateBorder = dtoContent.PlateBorder;

        storedData.AvatarBorder = dtoContent.AvatarBorder;
        storedData.AvatarOverlay = dtoContent.AvatarOverlay;

        storedData.DescriptionBG = dtoContent.DescriptionBG;
        storedData.DescriptionBorder = dtoContent.DescriptionBorder;
        storedData.DescriptionOverlay = dtoContent.DescriptionOverlay;

        storedData.GagSlotBG = dtoContent.GagSlotBG;
        storedData.GagSlotBorder = dtoContent.GagSlotBorder;
        storedData.GagSlotOverlay = dtoContent.GagSlotOverlay;

        storedData.PadlockBG = dtoContent.PadlockBG;
        storedData.PadlockBorder = dtoContent.PadlockBorder;
        storedData.PadlockOverlay = dtoContent.PadlockOverlay;

        storedData.BlockedSlotsBG = dtoContent.BlockedSlotsBG;
        storedData.BlockedSlotsBorder = dtoContent.BlockedSlotsBorder;
        storedData.BlockedSlotsOverlay = dtoContent.BlockedSlotsOverlay;

        storedData.BlockedSlotBorder = dtoContent.BlockedSlotBorder;
        storedData.BlockedSlotOverlay = dtoContent.BlockedSlotOverlay;
    }

    public static KinkPlateContent FromProfileData(this UserProfile data)
        => new()
        {
            Flagged = data.FlaggedForReport,
            Description = data.Description,
            CompletedTotal = data.AchievementsEarned,
            ChosenTitleId = data.ChosenTitleId,

            PlateBG = data.PlateBG,
            PlateBorder = data.PlateBorder,

            AvatarBorder = data.AvatarBorder,
            AvatarOverlay = data.AvatarOverlay,

            DescriptionBG = data.DescriptionBG,
            DescriptionBorder = data.DescriptionBorder,
            DescriptionOverlay = data.DescriptionOverlay,

            GagSlotBG = data.GagSlotBG,
            GagSlotBorder = data.GagSlotBorder,
            GagSlotOverlay = data.GagSlotOverlay,

            PadlockBG = data.PadlockBG,
            PadlockBorder = data.PadlockBorder,
            PadlockOverlay = data.PadlockOverlay,

            BlockedSlotsBG = data.BlockedSlotsBG,
            BlockedSlotsBorder = data.BlockedSlotsBorder,
            BlockedSlotsOverlay = data.BlockedSlotsOverlay,

            BlockedSlotBorder = data.BlockedSlotBorder,
            BlockedSlotOverlay = data.BlockedSlotOverlay
        };

    public static KinksterRequest ToApi(this PairingRequest r)
        => new(new(r.UserUID), new(r.OtherUserUID), r.IsTemporary, r.AttachedMessage, r.CreationTime);

    public static KinksterRequest ToApiRemoval(UserData user, UserData target)
        => new(user, target, false, string.Empty, DateTime.MinValue);

    public static UserData ToUserData(this User u)
        => new(u.UID, u.Alias, u.DisplayName, u.NameColor, u.NameGlowColor, u.Tier, u.CreatedAt);

    /// <summary>
    ///   Copies all properties over from two objects of the same <typeparamref name="T"/> type. <para />
    ///   This honestly might be better an an API method lol.
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