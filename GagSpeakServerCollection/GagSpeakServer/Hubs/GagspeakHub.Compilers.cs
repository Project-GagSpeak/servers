using GagspeakAPI.Data.Character;
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
    private GagspeakAPI.Data.Permissions.UserGlobalPermissions CompileGlobalPerms(UserGlobalPermissions userGlobalPermissions)
    {
        return new GagspeakAPI.Data.Permissions.UserGlobalPermissions()
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
            GagSlots = new GagSlot[3]
            {
                new GagSlot()
                {
                    GagType = appearanceData.SlotOneGagType,
                    Padlock = appearanceData.SlotOneGagPadlock,
                    Password = appearanceData.SlotOneGagPassword,
                    Timer = appearanceData.SlotOneGagTimer,
                    Assigner = appearanceData.SlotOneGagAssigner,
                },
                new GagSlot()
                {
                    GagType = appearanceData.SlotTwoGagType,
                    Padlock = appearanceData.SlotTwoGagPadlock,
                    Password = appearanceData.SlotTwoGagPassword,
                    Timer = appearanceData.SlotTwoGagTimer,
                    Assigner = appearanceData.SlotTwoGagAssigner,
                },
                new GagSlot()
                {
                    GagType = appearanceData.SlotThreeGagType,
                    Padlock = appearanceData.SlotThreeGagPadlock,
                    Password = appearanceData.SlotThreeGagPassword,
                    Timer = appearanceData.SlotThreeGagTimer,
                    Assigner = appearanceData.SlotThreeGagAssigner,
                }
            }
        };
    }
}

