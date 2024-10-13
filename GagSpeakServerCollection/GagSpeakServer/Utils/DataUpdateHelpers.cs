using GagspeakAPI.Data.Character;
using GagspeakAPI.Enums;
using GagspeakAPI.Extensions;
using GagspeakShared.Models;
using Microsoft.IdentityModel.Tokens;

namespace GagspeakServer;

// A collection of subtle helper functions to help minimize the bloat in the main gagspeak Hub.
public static class DataUpdateHelpers
{
    public static void UpdateGagState(this UserGagAppearanceData data, GagLayer layer, GagType type)
    {
        switch (layer)
        {
            case GagLayer.UnderLayer: data.SlotOneGagType = type.GagName(); break;
            case GagLayer.MiddleLayer: data.SlotTwoGagType = type.GagName(); break;
            case GagLayer.TopLayer: data.SlotThreeGagType = type.GagName(); break;
            default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
        }
    }

    public static bool CanApplyGag(UserGagAppearanceData gagData, GagLayer layer)
    {
        switch (layer)
        {
            case GagLayer.UnderLayer:
                return string.Equals(gagData.SlotOneGagPadlock, Padlocks.None.ToName(), StringComparison.Ordinal);
            case GagLayer.MiddleLayer:
                return string.Equals(gagData.SlotTwoGagPadlock, Padlocks.None.ToName(), StringComparison.Ordinal);
            case GagLayer.TopLayer:
                return string.Equals(gagData.SlotThreeGagPadlock, Padlocks.None.ToName(), StringComparison.Ordinal);
            default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
        }
    }

    // Your elementary true false spam, if it works, it works i guess.
    public static bool CanLockGag(UserGagAppearanceData gagData, ClientPairPermissions perms, GagLayer layer, out string ErrorMsg)
    {
        string gagType = layer switch
        {
            GagLayer.UnderLayer => gagData.SlotOneGagType,
            GagLayer.MiddleLayer => gagData.SlotTwoGagType,
            GagLayer.TopLayer => gagData.SlotThreeGagType,
            _ => gagData.SlotOneGagType
        };
        string padlock = layer switch
        {
            GagLayer.UnderLayer => gagData.SlotOneGagPadlock,
            GagLayer.MiddleLayer => gagData.SlotTwoGagPadlock,
            GagLayer.TopLayer => gagData.SlotThreeGagPadlock,
            _ => gagData.SlotOneGagPadlock
        };

        ErrorMsg = string.Empty;
        if (string.Equals(gagType, GagType.None.GagName(), StringComparison.Ordinal))
        {
            ErrorMsg = "No Gag Equipped on " + layer.ToString() + "!";
            return false;
        }
        else if (!string.Equals(padlock, Padlocks.None.ToName(), StringComparison.Ordinal))
        {
            ErrorMsg = "The " + layer.ToString() + " is already locked!";
            return false;
        }
        else if (padlock.ToPadlock() is Padlocks.OwnerPadlock or Padlocks.OwnerTimerPadlock && !perms.OwnerLocks)
        {
            ErrorMsg = "Owner Locks not allowed!";
            return false;
        }
        else if (padlock.ToPadlock() is Padlocks.DevotionalPadlock or Padlocks.DevotionalTimerPadlock && !perms.DevotionalLocks)
        {
            ErrorMsg = "Devotional Locks not allowed!";
            return false;
        }

        return true;
    }

    public static bool CanUnlockGag(UserGagAppearanceData gagData, ClientPairPermissions perms, GagSlot gagSlot, GagLayer layer, out string ErrorMsg)
    {
        string password = layer switch
        {
            GagLayer.UnderLayer => gagData.SlotOneGagPassword,
            GagLayer.MiddleLayer => gagData.SlotTwoGagPassword,
            GagLayer.TopLayer => gagData.SlotThreeGagPassword,
            _ => gagData.SlotOneGagPassword
        };
        string lockAssigner = layer switch
        {
            GagLayer.UnderLayer => gagData.SlotOneGagAssigner,
            GagLayer.MiddleLayer => gagData.SlotTwoGagAssigner,
            GagLayer.TopLayer => gagData.SlotThreeGagAssigner,
            _ => gagData.SlotOneGagAssigner
        };
        ErrorMsg = string.Empty;
        switch (gagSlot.Padlock.ToPadlock())
        {
            case Padlocks.None:
                ErrorMsg = "No Lock to Unlock!";
                return false; // No need to unlock if there is no lock.
            case Padlocks.FiveMinutesPadlock:
            case Padlocks.MetalPadlock:
                return true; // Always allow since there is no need for anything.
            case Padlocks.CombinationPadlock:
            case Padlocks.PasswordPadlock:
            case Padlocks.TimerPasswordPadlock:
                if (!string.Equals(password, gagSlot.Password, StringComparison.Ordinal))
                {
                    ErrorMsg = "Password does not match!";
                    return false;
                }
                return true;
            case Padlocks.OwnerPadlock:
            case Padlocks.OwnerTimerPadlock:
                if (!perms.OwnerLocks) // Defines if they have permission to remove "Owner" locks.
                {
                    ErrorMsg = "Cannot Unlock Owner Padlocks. Not Allowed!";
                    return false;
                }
                return true;
            case Padlocks.DevotionalPadlock:
            case Padlocks.DevotionalTimerPadlock:
                if (!string.Equals(lockAssigner, gagSlot.Assigner, StringComparison.Ordinal))
                {
                    ErrorMsg = "You did not Assign this Devotional Padlock. Not Allowed!";
                    return false;
                }
                return true;
            case Padlocks.MimicPadlock: return true; // Always allow unlock because it will always be done by automation.
            default: throw new ArgumentOutOfRangeException(nameof(gagSlot), gagSlot.Padlock, null);
        }
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

    // while the type conversions are not necessary, it ensures a valid state is always set.
    public static void GagLockUpdate(this UserGagAppearanceData data, GagLayer layer, Padlocks padlock, string pwd, string assigner, DateTimeOffset endTime)
    {
        // do not do anything with these locks
        if (padlock is Padlocks.None or Padlocks.MetalPadlock)
            return;

        if (padlock is Padlocks.CombinationPadlock or Padlocks.PasswordPadlock or Padlocks.TimerPasswordPadlock)
        {
            switch (layer)
            {
                case GagLayer.UnderLayer: data.SlotOneGagPassword = pwd; break;
                case GagLayer.MiddleLayer: data.SlotTwoGagPassword = pwd; break;
                case GagLayer.TopLayer: data.SlotThreeGagPassword = pwd; break;
                default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
            }
        }
        else if (padlock is Padlocks.FiveMinutesPadlock or Padlocks.TimerPasswordPadlock
            or Padlocks.OwnerTimerPadlock or Padlocks.DevotionalTimerPadlock or Padlocks.MimicPadlock)
        {
            switch (layer)
            {
                case GagLayer.UnderLayer: data.SlotOneGagTimer = endTime; break;
                case GagLayer.MiddleLayer: data.SlotTwoGagTimer = endTime; break;
                case GagLayer.TopLayer: data.SlotThreeGagTimer = endTime; break;
                default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
            }
        }

        // Assign the Assigner regardless.
        switch (layer)
        {
            case GagLayer.UnderLayer:
                data.SlotOneGagPadlock = padlock.ToName();
                data.SlotOneGagAssigner = assigner;
                break;
            case GagLayer.MiddleLayer:
                data.SlotTwoGagPadlock = padlock.ToName();
                data.SlotTwoGagAssigner = assigner;
                break;
            case GagLayer.TopLayer:
                data.SlotThreeGagPadlock = padlock.ToName();
                data.SlotThreeGagAssigner = assigner;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
        }
    }

    public static void GagUnlockUpdate(this UserGagAppearanceData data, GagLayer layer)
    {
        switch (layer)
        {
            case GagLayer.UnderLayer:
                data.SlotOneGagPadlock = Padlocks.None.ToName();
                data.SlotOneGagPassword = string.Empty;
                data.SlotOneGagTimer = DateTimeOffset.UtcNow;
                data.SlotOneGagAssigner = string.Empty;
                break;
            case GagLayer.MiddleLayer:
                data.SlotTwoGagPadlock = Padlocks.None.ToName();
                data.SlotTwoGagPassword = string.Empty;
                data.SlotTwoGagTimer = DateTimeOffset.UtcNow;
                data.SlotTwoGagAssigner = string.Empty;
                break;
            case GagLayer.TopLayer:
                data.SlotThreeGagPadlock = Padlocks.None.ToName();
                data.SlotThreeGagPassword = string.Empty;
                data.SlotThreeGagTimer = DateTimeOffset.UtcNow;
                data.SlotThreeGagAssigner = string.Empty;
                break;
            default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
        }
    }

    public static void UpdateRestraintActiveSet(this UserActiveStateData activeState, CharacterWardrobeData newData)
    {
        activeState.WardrobeActiveSetName = newData.ActiveSetName;
        activeState.WardrobeActiveSetAssigner = newData.ActiveSetEnabledBy;
    }

    public static bool CanLockRestraint(UserActiveStateData data, ClientPairPermissions perms, CharacterWardrobeData dtoData, out string ErrorMsg)
    {
        if (!perms.LockRestraintSets)
        {
            ErrorMsg = "Permission to Lock Restraint Sets not given!";
            return false;
        }
        else if (data.WardrobeActiveSetName.IsNullOrEmpty())
        {
            ErrorMsg = "No Active Set to Lock!";
            return false;
        }
        else if (data.WardrobeActiveSetPadLock.ToPadlock() is not Padlocks.None)
        {
            ErrorMsg = "Set is already Locked!";
            return false;
        }
        else if (dtoData.ActiveSetName.ToPadlock() is Padlocks.OwnerPadlock or Padlocks.OwnerTimerPadlock && !perms.OwnerLocks)
        {
            ErrorMsg = "Cannot Lock Owner Padlocks. Not Allowed!";
            return false;
        }
        else if (dtoData.ActiveSetName.ToPadlock() is Padlocks.DevotionalPadlock or Padlocks.DevotionalTimerPadlock && !perms.DevotionalLocks)
        {
            ErrorMsg = "Cannot Lock Devotional Padlocks. Not Allowed!";
            return false;
        }

        ErrorMsg = string.Empty;
        return true;
    }

    public static bool CanUnlockRestraint(UserActiveStateData stateData, ClientPairPermissions perms, CharacterWardrobeData lockInfo, out string ErrorMsg)
    {
        ErrorMsg = string.Empty;
        if (!perms.UnlockRestraintSets)
        {
            ErrorMsg = "Permission to Unlock Restraint Sets not given!";
            return false;
        }
        else if (stateData.WardrobeActiveSetPadLock.ToPadlock() is Padlocks.None)
        {
            ErrorMsg = "No Lock Present!";
            return false;
        }
        switch (stateData.WardrobeActiveSetPadLock.ToPadlock())
        {
            case Padlocks.None:
                ErrorMsg = "No Lock to Unlock!";
                return false; // No need to unlock if there is no lock.
            case Padlocks.FiveMinutesPadlock:
            case Padlocks.MetalPadlock:
                return true; // Always allow since there is no need for anything.
            case Padlocks.CombinationPadlock:
            case Padlocks.PasswordPadlock:
            case Padlocks.TimerPasswordPadlock:
                if (!string.Equals(stateData.WardrobeActiveSetPassword, lockInfo.Password, StringComparison.Ordinal))
                {
                    ErrorMsg = "Password does not match!";
                    return false;
                }
                return true;
            case Padlocks.OwnerPadlock:
            case Padlocks.OwnerTimerPadlock:
                if (!perms.OwnerLocks) // Defines if they have permission to remove "Owner" locks.
                {
                    ErrorMsg = "Cannot Unlock Owner Padlocks. Not Allowed!";
                    return false;
                }
                return true;
            case Padlocks.DevotionalPadlock:
            case Padlocks.DevotionalTimerPadlock:
                if (!perms.OwnerLocks) // Defines if they have permission to remove "Owner" locks.
                {
                    ErrorMsg = "Cannot Unlock Owner Padlocks. Not Allowed!";
                    return false;
                }
                else if (!string.Equals(stateData.WardrobeActiveSetLockAssigner, lockInfo.Assigner, StringComparison.Ordinal))
                {
                    ErrorMsg = "You did not Assign this Devotional Padlock. Not Allowed!";
                    return false;
                }
                return true;
            case Padlocks.MimicPadlock: return true; // Always allow unlock because it will always be done by automation.
            default:
                throw new ArgumentOutOfRangeException(nameof(lockInfo), lockInfo.Padlock, null);
        }
    }

    public static void RestraintLockUpdate(this UserActiveStateData activeState, Padlocks padlock, string password, string assigner, DateTimeOffset offsetTime)
    {
        // do not do anything with these locks
        if (padlock is Padlocks.None or Padlocks.MetalPadlock)
            return;

        if (padlock is Padlocks.CombinationPadlock or Padlocks.PasswordPadlock or Padlocks.TimerPasswordPadlock)
            activeState.WardrobeActiveSetPassword = password;
        else if (padlock is Padlocks.FiveMinutesPadlock or Padlocks.TimerPasswordPadlock or Padlocks.OwnerTimerPadlock or Padlocks.DevotionalTimerPadlock)
            activeState.WardrobeActiveSetLockTime = offsetTime;

        // Assign lock and assigner regardless.
        activeState.WardrobeActiveSetPadLock = padlock.ToName();
        activeState.WardrobeActiveSetLockAssigner = assigner;
    }

    public static void RestraintUnlockUpdate(this UserActiveStateData activeState)
    {
        activeState.WardrobeActiveSetPadLock = Padlocks.None.ToName();
        activeState.WardrobeActiveSetPassword = string.Empty;
        activeState.WardrobeActiveSetLockTime = DateTimeOffset.UtcNow;
        activeState.WardrobeActiveSetLockAssigner = string.Empty;
    }

    public static CharacterWardrobeData BuildUpdatedWardrobeData(CharacterWardrobeData prevData, UserActiveStateData userActiveState)
    {
        return new CharacterWardrobeData
        {
            OutfitNames = prevData.OutfitNames, // this becomes irrelevant since none of these settings change this.
            ActiveSetName = userActiveState.WardrobeActiveSetName,
            ActiveSetEnabledBy = userActiveState.WardrobeActiveSetAssigner,
            Padlock = userActiveState.WardrobeActiveSetPadLock,
            Password = userActiveState.WardrobeActiveSetPassword,
            Timer = userActiveState.WardrobeActiveSetLockTime,
            Assigner = userActiveState.WardrobeActiveSetLockAssigner,
        };
    }
}