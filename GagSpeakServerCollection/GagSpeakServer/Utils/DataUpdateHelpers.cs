using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Data.Interfaces;
using GagspeakAPI.Data.Struct;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Enums;
using GagspeakAPI.Extensions;
using GagspeakShared.Models;

namespace GagspeakServer;

// A collection of subtle helper functions to help minimize the bloat in the main gagspeak Hub.
public static class DataUpdateHelpers
{
    public static GagType GetGagType(this UserGagData data, GagLayer layer)
    {
        return layer switch
        {
            GagLayer.UnderLayer => data.SlotOneGagType.ToGagType(),
            GagLayer.MiddleLayer => data.SlotTwoGagType.ToGagType(),
            GagLayer.TopLayer => data.SlotThreeGagType.ToGagType(),
            _ => GagType.None
        };
    }

    public static Padlocks GetGagPadlock(this UserGagData data, GagLayer layer)
    {
        return layer switch
        {
            GagLayer.UnderLayer => data.SlotOneGagPadlock.ToPadlock(),
            GagLayer.MiddleLayer => data.SlotTwoGagPadlock.ToPadlock(),
            GagLayer.TopLayer => data.SlotThreeGagPadlock.ToPadlock(),
            _ => Padlocks.None
        };
    }

    public static void NewGagType(this UserGagData data, GagLayer layer, string gagType)
    {
        switch (layer)
        {
            case GagLayer.UnderLayer: data.SlotOneGagType = gagType; break;
            case GagLayer.MiddleLayer: data.SlotTwoGagType = gagType; break;
            case GagLayer.TopLayer: data.SlotThreeGagType = gagType; break;
            default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
        }
    }

    public static void NewPadlock(this UserGagData data, GagLayer layer, string padlock)
    {
        switch (layer)
        {
            case GagLayer.UnderLayer: data.SlotOneGagPadlock = padlock; break;
            case GagLayer.MiddleLayer: data.SlotTwoGagPadlock = padlock; break;
            case GagLayer.TopLayer: data.SlotThreeGagPadlock = padlock; break;
            default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
        }
    }

    public static void NewPassword(this UserGagData data, GagLayer layer, string password)
    {
        switch (layer)
        {
            case GagLayer.UnderLayer: data.SlotOneGagPassword = password; break;
            case GagLayer.MiddleLayer: data.SlotTwoGagPassword = password; break;
            case GagLayer.TopLayer: data.SlotThreeGagPassword = password; break;
            default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
        }
    }

    public static void NewTimer(this UserGagData data, GagLayer layer, DateTimeOffset releaseTime)
    {
        switch (layer)
        {
            case GagLayer.UnderLayer: data.SlotOneGagTimer = releaseTime; break;
            case GagLayer.MiddleLayer: data.SlotTwoGagTimer = releaseTime; break;
            case GagLayer.TopLayer: data.SlotThreeGagTimer = releaseTime; break;
            default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
        }
    }

    public static void NewAssigner(this UserGagData data, GagLayer layer, string assigner)
    {
        switch (layer)
        {
            case GagLayer.UnderLayer: data.SlotOneGagAssigner = assigner; break;
            case GagLayer.MiddleLayer: data.SlotTwoGagAssigner = assigner; break;
            case GagLayer.TopLayer: data.SlotThreeGagAssigner = assigner; break;
            default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
        }
    }

    public static bool CanApplyOrLockGag(this UserGagData gagData, GagLayer layer)
    {
        return layer switch
        {
            GagLayer.UnderLayer => string.Equals(gagData.SlotOneGagPadlock, Padlocks.None.ToName(), StringComparison.Ordinal),
            GagLayer.MiddleLayer => string.Equals(gagData.SlotTwoGagPadlock, Padlocks.None.ToName(), StringComparison.Ordinal),
            GagLayer.TopLayer => string.Equals(gagData.SlotThreeGagPadlock, Padlocks.None.ToName(), StringComparison.Ordinal),
            _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, null)
        };
    }

    public static bool CanRemoveGag(this UserGagData data, GagLayer layer)
    {
        switch (layer)
        {
            case GagLayer.UnderLayer: return !string.Equals(data.SlotOneGagPadlock, Padlocks.None.ToName(), StringComparison.Ordinal);
            case GagLayer.MiddleLayer: return !string.Equals(data.SlotTwoGagPadlock, Padlocks.None.ToName(), StringComparison.Ordinal);
            case GagLayer.TopLayer: return !string.Equals(data.SlotThreeGagPadlock, Padlocks.None.ToName(), StringComparison.Ordinal);
            default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
        }
    }

    public static GagSlot ToGagSlot(this UserGagData data, GagLayer layer)
    {
        switch (layer)
        {
            case GagLayer.UnderLayer: return new GagSlot() { GagType = data.SlotOneGagType, Padlock = data.SlotOneGagPadlock, Password = data.SlotOneGagPassword, Timer = data.SlotOneGagTimer, Assigner = data.SlotOneGagAssigner };
            case GagLayer.MiddleLayer: return new GagSlot() { GagType = data.SlotTwoGagType, Padlock = data.SlotTwoGagPadlock, Password = data.SlotTwoGagPassword, Timer = data.SlotTwoGagTimer, Assigner = data.SlotTwoGagAssigner };
            case GagLayer.TopLayer: return new GagSlot() { GagType = data.SlotThreeGagType, Padlock = data.SlotThreeGagPadlock, Password = data.SlotThreeGagPassword, Timer = data.SlotThreeGagTimer, Assigner = data.SlotThreeGagAssigner };
            default: return new GagSlot();
        }
    }

    public static bool CanApplyRestraint(this UserRestraintData data) => !data.ActiveSetId.IsEmptyGuid() && data.Padlock.ToPadlock() is not Padlocks.None;
    public static bool CanLockRestraint(this UserRestraintData data) => data.Padlock.ToPadlock() is Padlocks.None;
    public static bool CanUnlockRestraint(this UserRestraintData data) => data.Padlock.ToPadlock() is not Padlocks.None;
    public static bool CanRemoveRestraint(this UserRestraintData data) => data.Padlock.ToPadlock() is Padlocks.None && !data.ActiveSetId.IsEmptyGuid();
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
    {
        return new KinkPlateContent()
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
    }

    public static UserProfileData NewPlateFromDto(this UserKinkPlateDto dto)
    {
        return new UserProfileData()
        {
            UserUID = dto.User.UID,
            Base64ProfilePic = dto.ProfilePictureBase64,
            ProfileIsPublic = dto.Info.PublicPlate,
            UserDescription = dto.Info.Description,
            CompletedAchievementsTotal = dto.Info.CompletedAchievementsTotal,
            ChosenTitleId = dto.Info.ChosenTitleId,

            PlateBackground = dto.Info.PlateBackground,
            PlateBorder = dto.Info.PlateBorder,

            ProfilePictureBorder = dto.Info.ProfilePictureBorder,
            ProfilePictureOverlay = dto.Info.ProfilePictureOverlay,

            DescriptionBackground = dto.Info.DescriptionBackground,
            DescriptionBorder = dto.Info.DescriptionBorder,
            DescriptionOverlay = dto.Info.DescriptionOverlay,

            GagSlotBackground = dto.Info.GagSlotBackground,
            GagSlotBorder = dto.Info.GagSlotBorder,
            GagSlotOverlay = dto.Info.GagSlotOverlay,

            PadlockBackground = dto.Info.PadlockBackground,
            PadlockBorder = dto.Info.PadlockBorder,
            PadlockOverlay = dto.Info.PadlockOverlay,

            BlockedSlotsBackground = dto.Info.BlockedSlotsBackground,
            BlockedSlotsBorder = dto.Info.BlockedSlotsBorder,
            BlockedSlotsOverlay = dto.Info.BlockedSlotsOverlay,

            BlockedSlotBorder = dto.Info.BlockedSlotBorder,
            BlockedSlotOverlay = dto.Info.BlockedSlotOverlay
        };
    }
}