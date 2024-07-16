using GagSpeakAPI.Data;
using GagSpeakAPI.Data.Enum;
using GagspeakShared.Models;
using Microsoft.EntityFrameworkCore;
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
}