using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Data.Interfaces;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Enums;
using GagspeakAPI.Extensions;
using GagspeakShared.Models;

namespace GagspeakServer;

// A collection of subtle helper functions to help minimize the bloat in the main gagspeak Hub.
public static class DataUpdateHelpers
{
    public static void NewGagType(this UserGagAppearanceData data, GagLayer layer, string gagType)
    {
        switch (layer)
        {
            case GagLayer.UnderLayer: data.SlotOneGagType = gagType; break;
            case GagLayer.MiddleLayer: data.SlotTwoGagType = gagType; break;
            case GagLayer.TopLayer: data.SlotThreeGagType = gagType; break;
            default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
        }
    }

    public static void NewPadlock(this UserGagAppearanceData data, GagLayer layer, string padlock)
    {
        switch (layer)
        {
            case GagLayer.UnderLayer: data.SlotOneGagPadlock = padlock; break;
            case GagLayer.MiddleLayer: data.SlotTwoGagPadlock = padlock; break;
            case GagLayer.TopLayer: data.SlotTwoGagPadlock = padlock; break;
            default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
        }
    }

    public static void NewPassword(this UserGagAppearanceData data, GagLayer layer, string password)
    {
        switch (layer)
        {
            case GagLayer.UnderLayer: data.SlotOneGagPassword = password; break;
            case GagLayer.MiddleLayer: data.SlotTwoGagPassword = password; break;
            case GagLayer.TopLayer: data.SlotThreeGagPassword = password; break;
            default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
        }
    }

    public static void NewTimer(this UserGagAppearanceData data, GagLayer layer, DateTimeOffset releaseTime)
    {
        switch (layer)
        {
            case GagLayer.UnderLayer: data.SlotOneGagTimer = releaseTime; break;
            case GagLayer.MiddleLayer: data.SlotTwoGagTimer = releaseTime; break;
            case GagLayer.TopLayer: data.SlotThreeGagTimer = releaseTime; break;
            default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
        }
    }

    public static void NewAssigner(this UserGagAppearanceData data, GagLayer layer, string assigner)
    {
        switch (layer)
        {
            case GagLayer.UnderLayer: data.SlotOneGagAssigner = assigner; break;
            case GagLayer.MiddleLayer: data.SlotTwoGagAssigner = assigner; break;
            case GagLayer.TopLayer: data.SlotThreeGagAssigner = assigner; break;
            default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
        }
    }

    public static bool CanApplyOrLockGag(this UserGagAppearanceData gagData, GagLayer layer)
    {
        return layer switch
        {
            GagLayer.UnderLayer => string.Equals(gagData.SlotOneGagPadlock, Padlocks.None.ToName(), StringComparison.Ordinal),
            GagLayer.MiddleLayer => string.Equals(gagData.SlotTwoGagPadlock, Padlocks.None.ToName(), StringComparison.Ordinal),
            GagLayer.TopLayer => string.Equals(gagData.SlotThreeGagPadlock, Padlocks.None.ToName(), StringComparison.Ordinal),
            _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, null)
        };
    }

    public static PadlockReturnCode IsLockUpdateValid(this CharaAppearanceData dtoData, GagLayer layer, ClientPairPermissions perms)
    {
        var slotToCheck = dtoData.GagSlots[(int)layer];
        return ValidateLock(slotToCheck.Padlock.ToPadlock(), slotToCheck.Password, slotToCheck.Timer, perms.MaxGagTime, perms);
    }

    public static PadlockReturnCode IsLockUpdateValid(this CharaWardrobeData dtoData, ClientPairPermissions perms)
    {
        return ValidateLock(dtoData.Padlock.ToPadlock(), dtoData.Password, dtoData.Timer, perms.MaxAllowedRestraintTime, perms);
    }

    // A method taken from the API using the server model.
    public static PadlockReturnCode ValidateLock(Padlocks lockDesired, string pass, DateTimeOffset time, TimeSpan maxLockTime, ClientPairPermissions perms)
    {
        var returnCode = PadlockReturnCode.Success;

        if (lockDesired is Padlocks.None)
            return returnCode |= PadlockReturnCode.NoPadlockSelected;

        var validationRules = new Dictionary<Padlocks, Func<PadlockReturnCode>>
        {
            { Padlocks.MetalPadlock, () => PadlockReturnCode.Success },
            { Padlocks.FiveMinutesPadlock, () => PadlockReturnCode.Success },
            { Padlocks.CombinationPadlock, () =>
                !GsPadlockEx.IsValidCombo(pass) ? PadlockReturnCode.InvalidCombination :
                !perms.PermanentLocks ? PadlockReturnCode.PermanentRestricted : PadlockReturnCode.Success },
            { Padlocks.PasswordPadlock, () =>
                !GsPadlockEx.IsValidPass(pass) ? PadlockReturnCode.InvalidPassword :
                !perms.PermanentLocks ? PadlockReturnCode.PermanentRestricted : PadlockReturnCode.Success },
            { Padlocks.TimerPadlock, () =>
                (time - DateTimeOffset.UtcNow > maxLockTime) ? PadlockReturnCode.InvalidTime : PadlockReturnCode.Success },
            { Padlocks.TimerPasswordPadlock, () =>
                !GsPadlockEx.IsValidPass(pass) ? PadlockReturnCode.InvalidPassword :
                (time - DateTimeOffset.UtcNow > maxLockTime) ? PadlockReturnCode.InvalidTime : PadlockReturnCode.Success },
            { Padlocks.OwnerPadlock, () =>
                !perms.OwnerLocks ? PadlockReturnCode.OwnerRestricted :
                !perms.PermanentLocks ? PadlockReturnCode.PermanentRestricted : PadlockReturnCode.Success },
            { Padlocks.OwnerTimerPadlock, () =>
                !perms.OwnerLocks ? PadlockReturnCode.OwnerRestricted :
                !(time - DateTimeOffset.UtcNow > maxLockTime) ? PadlockReturnCode.InvalidTime : PadlockReturnCode.Success },
            { Padlocks.DevotionalPadlock, () =>
                !perms.DevotionalLocks ? PadlockReturnCode.DevotionalRestricted :
                !perms.PermanentLocks ? PadlockReturnCode.PermanentRestricted : PadlockReturnCode.Success },
            { Padlocks.DevotionalTimerPadlock, () =>
                !perms.DevotionalLocks ? PadlockReturnCode.DevotionalRestricted :
                !(time - DateTimeOffset.UtcNow > maxLockTime) ? PadlockReturnCode.InvalidTime : PadlockReturnCode.Success }
        };

        // Check if validation rules exist for the padlock and return the corresponding error code
        if (validationRules.TryGetValue(lockDesired, out var validate))
            returnCode = validate();

        return returnCode;
    }

    public static PadlockReturnCode IsUnlockUpdateValid(this UserGagAppearanceData gagData, string enactorUID, GagLayer layer, GagSlot padlockableItem, ClientPairPermissions perms)
    {
        var currentPassword = layer switch
        {
            GagLayer.UnderLayer => gagData.SlotOneGagPassword,
            GagLayer.MiddleLayer => gagData.SlotTwoGagPassword,
            GagLayer.TopLayer => gagData.SlotThreeGagPassword,
            _ => gagData.SlotOneGagPassword
        };
        return ValidateUnlock(padlockableItem, new(gagData.UserUID), currentPassword, enactorUID, perms);
    }

    public static PadlockReturnCode IsUnlockUpdateValid(this UserActiveSetData setData, string enactorUID, CharaWardrobeData dtoData, ClientPairPermissions perms)
    {
        return ValidateUnlock(dtoData, new(setData.UserUID), setData.Password, enactorUID, perms);
    }

    public static PadlockReturnCode ValidateUnlock<T>(T item, UserData itemOwner, string guessedPass, string unlockerUID, ClientPairPermissions perms) where T : IPadlockable
    {
        var returnCode = PadlockReturnCode.Success;

        if (item is CharaWardrobeData && !perms.UnlockRestraintSets)
            return returnCode |= PadlockReturnCode.UnlockingRestricted;

        if (item is GagSlot && !perms.UnlockGags)
            return returnCode |= PadlockReturnCode.UnlockingRestricted;

        var validationRules = new Dictionary<Padlocks, Func<PadlockReturnCode>>
        {
            { Padlocks.MetalPadlock, () => PadlockReturnCode.Success },
            { Padlocks.FiveMinutesPadlock, () => PadlockReturnCode.Success },
            { Padlocks.TimerPadlock, () =>
                string.Equals(itemOwner.UID, unlockerUID, StringComparison.Ordinal) ? PadlockReturnCode.UnlockingRestricted : PadlockReturnCode.Success },
            { Padlocks.CombinationPadlock, () =>
                !string.Equals(item.Password, guessedPass, StringComparison.Ordinal) ? PadlockReturnCode.InvalidCombination : PadlockReturnCode.Success },
            { Padlocks.PasswordPadlock, () =>
                !string.Equals(item.Password, guessedPass, StringComparison.Ordinal) ? PadlockReturnCode.InvalidPassword : PadlockReturnCode.Success },
            { Padlocks.TimerPasswordPadlock, () =>
                !string.Equals(item.Password, guessedPass, StringComparison.Ordinal) ? PadlockReturnCode.InvalidPassword : PadlockReturnCode.Success },
            { Padlocks.OwnerPadlock, () =>
                !perms.OwnerLocks ? PadlockReturnCode.OwnerRestricted : PadlockReturnCode.Success },
            { Padlocks.OwnerTimerPadlock, () =>
                !perms.OwnerLocks ? PadlockReturnCode.OwnerRestricted : PadlockReturnCode.Success },
            { Padlocks.DevotionalPadlock, () =>
                !perms.DevotionalLocks ? PadlockReturnCode.DevotionalRestricted :
                !string.Equals(item.Assigner, unlockerUID, StringComparison.Ordinal) ? PadlockReturnCode.NotLockAssigner : PadlockReturnCode.Success },
            { Padlocks.DevotionalTimerPadlock, () =>
                !perms.DevotionalLocks ? PadlockReturnCode.DevotionalRestricted :
                !string.Equals(item.Assigner, unlockerUID, StringComparison.Ordinal) ? PadlockReturnCode.NotLockAssigner : PadlockReturnCode.Success }
        };

        // Check if validation rules exist for the padlock and return the corresponding error code
        if (validationRules.TryGetValue(item.Padlock.ToPadlock(), out var validate))
            returnCode = validate();

        return returnCode;
    }

    public static bool CanRemoveGag(this UserGagAppearanceData data, GagLayer layer)
    {
        switch (layer)
        {
            case GagLayer.UnderLayer: return !string.Equals(data.SlotOneGagPadlock, Padlocks.None.ToName(), StringComparison.Ordinal);
            case GagLayer.MiddleLayer: return !string.Equals(data.SlotTwoGagPadlock, Padlocks.None.ToName(), StringComparison.Ordinal);
            case GagLayer.TopLayer: return !string.Equals(data.SlotThreeGagPadlock, Padlocks.None.ToName(), StringComparison.Ordinal);
            default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
        }
    }

    public static CharaWardrobeData BuildUpdatedWardrobeData(CharaWardrobeData prevData, UserActiveSetData userActiveState)
    {
        return new CharaWardrobeData
        {
            ActiveSetId = prevData.ActiveSetId,
            ActiveSetEnabledBy = userActiveState.ActiveSetEnabler,
            Padlock = userActiveState.Padlock,
            Password = userActiveState.Password,
            Timer = userActiveState.Timer,
            Assigner = userActiveState.Assigner,
            ActiveCursedItems = prevData.ActiveCursedItems,
        };
    }

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