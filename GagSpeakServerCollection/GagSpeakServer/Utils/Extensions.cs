using GagspeakAPI.Data;
using GagspeakAPI.Data.Character;
using GagspeakAPI.Enums;
using GagspeakShared.Models;
using System.Reflection;
using static GagspeakServer.Hubs.GagspeakHub;

namespace GagspeakServer;

/// <summary>
/// Extensions for the IHttpContextAccessor
/// </summary>
public static class Extensions
{

    // an extention method for the userData 
    public static UserData ToUserData(this User user)
    {
        return new UserData(user.UID, string.IsNullOrWhiteSpace(user.Alias) ? null : user.Alias, user.VanityTier);
    }

    public static UserData ToUserDataFromUID(this string UserUID)
    {
        return new UserData(UserUID, null, null);
    }

    /// <summary> Fetch the individual pair status based on the userInfo </summary>
    public static IndividualPairStatus ToIndividualPairStatus(this UserInfo userInfo)
    {
        if (userInfo.IsSynced) return IndividualPairStatus.Bidirectional;
        if (!userInfo.IsSynced) return IndividualPairStatus.OneSided;
        return IndividualPairStatus.None;
    }

    public static void CopyPropertiesTo<T>(this T source, T target)
    {
        if (source == null || target == null)
            throw new ArgumentNullException("Source or/and target objects are null");

        var type = typeof(T);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (property.CanRead && property.CanWrite)
            {
                var value = property.GetValue(source, null);
                property.SetValue(target, value, null);
            }
        }
    }

    public static CharaAppearanceData ToApiAppearance(this UserGagAppearanceData data)
    => new CharaAppearanceData
    {
        GagSlots = new GagSlot[]
        {
            new GagSlot
            {
                GagType = data.SlotOneGagType,
                Padlock = data.SlotOneGagPadlock,
                Password = data.SlotOneGagPassword,
                Timer = data.SlotOneGagTimer,
                Assigner = data.SlotOneGagAssigner
            },
            new GagSlot
            {
                GagType = data.SlotTwoGagType,
                Padlock = data.SlotTwoGagPadlock,
                Password = data.SlotTwoGagPassword,
                Timer = data.SlotTwoGagTimer,
                Assigner = data.SlotTwoGagAssigner
            },
            new GagSlot
            {
                GagType = data.SlotThreeGagType,
                Padlock = data.SlotThreeGagPadlock,
                Password = data.SlotThreeGagPassword,
                Timer = data.SlotThreeGagTimer,
                Assigner = data.SlotThreeGagAssigner
            }
        }
    };
}