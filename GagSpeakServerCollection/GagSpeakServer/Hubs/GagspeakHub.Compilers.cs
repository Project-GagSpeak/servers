using GagSpeak.API.Data.Character;
using GagspeakShared.Models;

namespace GagspeakServer.Hubs;

/// <summary>
/// This partial class of the GagspeakHub handles helper functions used in the servers function responses to make them cleaner.
/// <para> AKA, This is the "Messy code file" </para>
/// <para> Also contains the Client Caller context UserCharaIdentity, the UserUID, and the Continent</para>
/// </summary>
public partial class GagspeakHub
{
    /// <summary> 
    /// Compiles a globalPermissions DTO with the data provided from the DbContext's UserGlobalPermissions 
    /// </summary>
    /// <returns> A GlobalPermissionsDto object </returns>
    private UserGlobalPermissions CompileGlobalPerms(UserGlobalPermissions userGlobalPermissions)
    {
        return new UserGlobalPermissions()
        {
            Safeword = userGlobalPermissions.Safeword,
            SafewordUsed = userGlobalPermissions.SafewordUsed,
            CommandsFromFriends = userGlobalPermissions.CommandsFromFriends,
            CommandsFromParty = userGlobalPermissions.CommandsFromParty,
            LiveChatGarblerActive = userGlobalPermissions.LiveChatGarblerActive,
            LiveChatGarblerLocked = userGlobalPermissions.LiveChatGarblerLocked,
            WardrobeEnabled = userGlobalPermissions.WardrobeEnabled,
            ItemAutoEquip = userGlobalPermissions.ItemAutoEquip,
            RestraintSetAutoEquip = userGlobalPermissions.RestraintSetAutoEquip,
            LockGagStorageOnGagLock = userGlobalPermissions.LockGagStorageOnGagLock,
            PuppeteerEnabled = userGlobalPermissions.PuppeteerEnabled,
            GlobalTriggerPhrase = userGlobalPermissions.GlobalTriggerPhrase,
            GlobalAllowSitRequests = userGlobalPermissions.GlobalAllowSitRequests,
            GlobalAllowMotionRequests = userGlobalPermissions.GlobalAllowMotionRequests,
            GlobalAllowAllRequests = userGlobalPermissions.GlobalAllowAllRequests,
            MoodlesEnabled = userGlobalPermissions.MoodlesEnabled,
            ToyboxEnabled = userGlobalPermissions.ToyboxEnabled,
            LockToyboxUI = userGlobalPermissions.LockToyboxUI,
            ToyIsActive = userGlobalPermissions.ToyIsActive,
            ToyIntensity = userGlobalPermissions.ToyIntensity,
            SpatialVibratorAudio = userGlobalPermissions.SpatialVibratorAudio,
        };
    }

    /// <summary>
    /// Compiles the CharacterAppearanceData object from the DbContext's UserGagAppearanceData (not the DTO variant)
    /// </summary>
    /// <returns> A CharacterAppearanceData object </returns>
    private CharacterAppearanceData CompileCharaAppearanceData(UserGagAppearanceData appearanceData)
    {
        return new CharacterAppearanceData()
        {
            SlotOneGagType = appearanceData.SlotOneGagType,
            SlotOneGagPadlock = appearanceData.SlotOneGagPadlock,
            SlotOneGagPassword = appearanceData.SlotOneGagPassword,
            SlotOneGagTimer = appearanceData.SlotOneGagTimer,
            SlotOneGagAssigner = appearanceData.SlotOneGagAssigner,
            SlotTwoGagType = appearanceData.SlotTwoGagType,
            SlotTwoGagPadlock = appearanceData.SlotTwoGagPadlock,
            SlotTwoGagPassword = appearanceData.SlotTwoGagPassword,
            SlotTwoGagTimer = appearanceData.SlotTwoGagTimer,
            SlotTwoGagAssigner = appearanceData.SlotTwoGagAssigner,
            SlotThreeGagType = appearanceData.SlotThreeGagType,
            SlotThreeGagPadlock = appearanceData.SlotThreeGagPadlock,
            SlotThreeGagPassword = appearanceData.SlotThreeGagPassword,
            SlotThreeGagTimer = appearanceData.SlotThreeGagTimer,
            SlotThreeGagAssigner = appearanceData.SlotThreeGagAssigner,
        };
    }
}

