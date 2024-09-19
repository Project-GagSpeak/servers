using GagspeakAPI.Data.Character;
using GagspeakAPI.Dto.User;
using GagspeakAPI.Enums;
using GagspeakAPI.Extensions;
using GagspeakShared.Models;
using Microsoft.IdentityModel.Tokens;
using System.Runtime.CompilerServices;
using System.Security.Policy;

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
                return string.Equals(gagData.SlotOneGagType, GagType.None.GagName(), StringComparison.Ordinal) 
                    && string.Equals(gagData.SlotOneGagPadlock, Padlocks.None.ToName(), StringComparison.Ordinal);
            case GagLayer.MiddleLayer:
                return string.Equals(gagData.SlotTwoGagType, GagType.None.GagName(), StringComparison.Ordinal) 
                    && string.Equals(gagData.SlotTwoGagPadlock, Padlocks.None.ToName(), StringComparison.Ordinal);
            case GagLayer.TopLayer:
                return string.Equals(gagData.SlotThreeGagType, GagType.None.GagName(), StringComparison.Ordinal) 
                    && string.Equals(gagData.SlotThreeGagPadlock, Padlocks.None.ToName(), StringComparison.Ordinal);
            default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
        }
    }

    // Your elementary true false spam, if it works, it works i guess.
    public static bool CanLockGag(UserGagAppearanceData gagData, ClientPairPermissions perms, GagLayer layer, out string ErrorMsg)
    {
        ErrorMsg = string.Empty;
        switch (layer)
        {
            case GagLayer.UnderLayer:
                if(string.Equals(gagData.SlotOneGagType, GagType.None.GagName(), StringComparison.Ordinal)) 
                { ErrorMsg = "No Gag Equipped!"; return false; }

                if (!string.Equals(gagData.SlotOneGagPadlock, Padlocks.None.ToName(), StringComparison.Ordinal))
                { ErrorMsg = "Slot One is already locked!"; return false; }

                if (gagData.SlotOneGagPadlock.ToPadlock() is Padlocks.OwnerPadlock or Padlocks.OwnerTimerPadlock && !perms.OwnerLocks)
                { ErrorMsg = "Owner Locks not allowed!"; return false; }

                return true;
            case GagLayer.MiddleLayer:
                if(string.Equals(gagData.SlotTwoGagType, GagType.None.GagName(), StringComparison.Ordinal)) 
                { ErrorMsg = "No Gag Equipped!"; return false; }

                if (!string.Equals(gagData.SlotTwoGagPadlock, Padlocks.None.ToName(), StringComparison.Ordinal))
                { ErrorMsg = "Slot Two is already locked!"; return false; }

                if (gagData.SlotTwoGagPadlock.ToPadlock() is Padlocks.OwnerPadlock or Padlocks.OwnerTimerPadlock && !perms.OwnerLocks)
                { ErrorMsg = "Owner Locks not allowed!"; return false; }

                return true;
            case GagLayer.TopLayer:
                if(string.Equals(gagData.SlotThreeGagType, GagType.None.GagName(), StringComparison.Ordinal)) 
                { ErrorMsg = "No Gag Equipped!"; return false; }

                if (!string.Equals(gagData.SlotThreeGagPadlock, Padlocks.None.ToName(), StringComparison.Ordinal))
                { ErrorMsg = "Slot Three is already locked!"; return false; }

                if(gagData.SlotThreeGagPadlock.ToPadlock() is Padlocks.OwnerPadlock or Padlocks.OwnerTimerPadlock && !perms.OwnerLocks)
                { ErrorMsg = "Owner Locks not allowed!"; return false; }

                return true;
            default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
        }
    }

    public static bool CanUnlockGag(UserGagAppearanceData gagData, ClientPairPermissions perms, GagSlot gagSlot, GagLayer layer, out string ErrorMsg)
    {
        ErrorMsg = string.Empty;
        switch (layer)
        {
            case GagLayer.UnderLayer:
                if (string.Equals(gagData.SlotOneGagType, GagType.None.GagName(), StringComparison.Ordinal))
                { ErrorMsg = "Slot One has no Gag Equipped. Cannot Unlock!"; return false; }

                if (!string.Equals(gagData.SlotOneGagPassword, gagSlot.Password, StringComparison.Ordinal))
                { ErrorMsg = "Password does not match!"; return false; }

                if (gagData.SlotOneGagPadlock.ToPadlock() is Padlocks.OwnerPadlock or Padlocks.OwnerTimerPadlock && !perms.OwnerLocks)
                { ErrorMsg = "Cannot Unlock Owner Padlocks. Not Allowed!"; return false; }

                return true;
            case GagLayer.MiddleLayer:
                if (string.Equals(gagData.SlotTwoGagType, GagType.None.GagName(), StringComparison.Ordinal))
                { ErrorMsg = "Slot Two has no Gag Equipped. Cannot Unlock!"; return false; }

                if (!string.Equals(gagData.SlotTwoGagPassword, gagSlot.Password, StringComparison.Ordinal))
                { ErrorMsg = "Password does not match!"; return false; }

                if (gagData.SlotTwoGagPadlock.ToPadlock() is Padlocks.OwnerPadlock or Padlocks.OwnerTimerPadlock && !perms.OwnerLocks)
                { ErrorMsg = "Cannot Unlock Owner Padlocks. Not Allowed!"; return false; }

                return true;
            case GagLayer.TopLayer:
                if (string.Equals(gagData.SlotThreeGagType, GagType.None.GagName(), StringComparison.Ordinal))
                { ErrorMsg = "Slot Three has no Gag Equipped. Cannot Unlock!"; return false; }

                if (!string.Equals(gagData.SlotThreeGagPassword, gagSlot.Password, StringComparison.Ordinal))
                { ErrorMsg = "Password does not match!"; return false; }

                if (gagData.SlotThreeGagPadlock.ToPadlock() is Padlocks.OwnerPadlock or Padlocks.OwnerTimerPadlock && !perms.OwnerLocks)
                { ErrorMsg = "Cannot Unlock Owner Padlocks. Not Allowed!"; return false; }

                return true;
            default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
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
    public static void UpdateGagLockState(this UserGagAppearanceData data, 
        GagLayer layer, Padlocks padlock, string password, string assigner, DateTimeOffset offsetTime)
    {
        switch (layer)
        {
            case GagLayer.UnderLayer:
                data.SlotOneGagPadlock = padlock.ToName();
                data.SlotOneGagPassword = password;
                data.SlotOneGagTimer = offsetTime;
                data.SlotOneGagAssigner = assigner;
                break;
            case GagLayer.MiddleLayer:
                data.SlotTwoGagPadlock = padlock.ToName();
                data.SlotTwoGagPassword = password;
                data.SlotTwoGagTimer = offsetTime;
                data.SlotTwoGagAssigner = assigner;
                break;
            case GagLayer.TopLayer:
                data.SlotThreeGagPadlock = padlock.ToName();
                data.SlotThreeGagPassword = password;
                data.SlotThreeGagTimer = offsetTime;
                data.SlotThreeGagAssigner = assigner;
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
        ErrorMsg = string.Empty;
        if(!perms.LockRestraintSets)
        { ErrorMsg = "Permission to Lock Restraint Sets not given!"; return false;}

        if(data.WardrobeActiveSetName.IsNullOrEmpty())
        { ErrorMsg = "No Active Set to Lock!"; return false; }

        if (data.WardrobeActiveSetPadLock.ToPadlock() is not Padlocks.None)
        { ErrorMsg = "Set is already Locked!"; return false; }

        if (dtoData.ActiveSetName.ToPadlock() is Padlocks.OwnerPadlock or Padlocks.OwnerTimerPadlock && !perms.OwnerLocks)
        { ErrorMsg = "Cannot Lock Owner Padlocks. Not Allowed!"; return false; }

        return true;
    }

    public static bool CanUnlockRestraint(UserActiveStateData stateData, ClientPairPermissions perms, CharacterWardrobeData lockInfo, out string ErrorMsg)
    {
        ErrorMsg = string.Empty;
        if (!perms.UnlockRestraintSets)
        { ErrorMsg = "Permission to Unlock Restraint Sets not given!"; return false; }

        if (stateData.WardrobeActiveSetPadLock.ToPadlock() is Padlocks.None)
        { ErrorMsg = "No Lock Present!"; return false; }

        if (!string.Equals(stateData.WardrobeActiveSetPassword, lockInfo.Password, StringComparison.Ordinal))
        { ErrorMsg = "Password does not match!"; return false; }

        if (stateData.WardrobeActiveSetPadLock.ToPadlock() is Padlocks.OwnerPadlock or Padlocks.OwnerTimerPadlock && !perms.OwnerLocks)
        { ErrorMsg = "Cannot Unlock Owner Padlocks. Not Allowed!"; return false; }

        return true;
    }

    public static void UpdateWardrobeSetLock(this UserActiveStateData activeState, Padlocks padlock, string password, string assigner, DateTimeOffset offsetTime)
    {
        activeState.WardrobeActiveSetPadLock = padlock.ToName();
        activeState.WardrobeActiveSetPassword = password;
        activeState.WardrobeActiveSetLockTime = offsetTime;
        activeState.WardrobeActiveSetAssigner = assigner;
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