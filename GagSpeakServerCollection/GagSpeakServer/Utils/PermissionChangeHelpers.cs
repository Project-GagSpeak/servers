using GagspeakShared.Models;

namespace GagspeakServer;

// A collection of subtle helper functions to help minimize the bloat in the main gagspeak Hub.
public static class PermissionChangeHelpers
{
    public static bool UpdateGlobalPerm(this UserGlobalPermissions data, KeyValuePair<string, object> changedPermission, out string error)
    {
        error = "Property to modify not found!"; // Default Error msg.
        // establish the key-value pair from the Dto so we know what is changing.
        string propertyName = changedPermission.Key;
        object newValue = changedPermission.Value;
        // Obtain the property info.
        var propertyInfo = typeof(UserGlobalPermissions).GetProperty(propertyName);
        if (propertyInfo is null)
            return false;

        // PropertyInfo is valid, so see if we can set it with the default reconition.
        if (propertyInfo.PropertyType == newValue.GetType())
        {
            propertyInfo.SetValue(data, newValue);
            return true;
        }
        // if it fails, attempt to recognize timespan.
        if (newValue is UInt64 && propertyInfo.PropertyType == typeof(TimeSpan))
        {
            long ticks = (long)(ulong)newValue;  // Safe cast from ulong to long
            propertyInfo.SetValue(data, TimeSpan.FromTicks(ticks));
            return true;
        }
        // or see if its a byte and equal to a char, for the start & end char characters.
        else if (newValue is byte && propertyInfo.PropertyType == typeof(char))
        {
            propertyInfo.SetValue(data, (char)(byte)newValue);
            return true;
        }

        // Output type miss-match error for logs.
        error = "Property type mismatch! PropertyType was: " + propertyInfo.PropertyType + ", but NewValueType: " + newValue.GetType();
        return false;
    }
    public static bool UpdatePairPerms(this ClientPairPermissions data, KeyValuePair<string, object> changedPermission, out string error)
    {
        error = "Property to modify not found!"; // Default Error msg.
        // establish the key-value pair from the Dto so we know what is changing.
        string propertyName = changedPermission.Key;
        object newValue = changedPermission.Value;
        // Obtain the property info.
        var propertyInfo = typeof(ClientPairPermissions).GetProperty(propertyName);
        if (propertyInfo is null)
            return false;

        // PropertyInfo is valid, so see if we can set it with the default reconition.
        if (propertyInfo.PropertyType == newValue.GetType())
        {
            propertyInfo.SetValue(data, newValue);
            return true;
        }
        // if it fails, attempt to reconize timespan.
        if (newValue is UInt64 && propertyInfo.PropertyType == typeof(TimeSpan))
        {
            long ticks = (long)(ulong)newValue;  // Safe cast from ulong to long
            propertyInfo.SetValue(data, TimeSpan.FromTicks(ticks));
            return true;
        }
        // or see if its a byte and equal to a char, for the start & end char characters.
        else if (newValue is byte && propertyInfo.PropertyType == typeof(char))
        {
            propertyInfo.SetValue(data, (char)(byte)newValue);
            return true;
        }

        // Output type missmatch error for logs.
        error = "Property type mismatch! PropertyType was: " + propertyInfo.PropertyType + ", but NewValueType: " + newValue.GetType();
        return false;
    }

    public static bool UpdatePairPermsAccess(this ClientPairPermissionAccess data, KeyValuePair<string, object> changedPermission, out string error)
    {
        error = "Property to modify not found!"; // Default Error msg.
        // establish the key-value pair from the Dto so we know what is changing.
        string propertyName = changedPermission.Key;
        object newValue = changedPermission.Value;
        // Obtain the property info.
        var propertyInfo = typeof(ClientPairPermissionAccess).GetProperty(propertyName);
        if (propertyInfo is null)
            return false;

        // PropertyInfo is valid, so see if we can set it with the default reconition.
        if (propertyInfo.PropertyType == newValue.GetType())
        {
            propertyInfo.SetValue(data, newValue);
            return true;
        }
        // if it fails, attempt to reconize timespan.
        if (newValue is UInt64 && propertyInfo.PropertyType == typeof(TimeSpan))
        {
            long ticks = (long)(ulong)newValue;  // Safe cast from ulong to long
            propertyInfo.SetValue(data, TimeSpan.FromTicks(ticks));
            return true;
        }
        // or see if its a byte and equal to a char, for the start & end char characters.
        else if (newValue is byte && propertyInfo.PropertyType == typeof(char))
        {
            propertyInfo.SetValue(data, (char)(byte)newValue);
            return true;
        }

        // Output type missmatch error for logs.
        error = "Property type mismatch! PropertyType was: " + propertyInfo.PropertyType + ", but NewValueType: " + newValue.GetType();
        return false;
    }
}