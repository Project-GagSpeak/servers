using Gagspeak.API.Data.Enum;
using Gagspeak.API.Data;
using GagspeakServer.Models;
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
        return new UserData(user.UID, user.Alias);
    }

    /// <summary> Fetch the individual pair status based on the userInfo </summary>
    public static IndividualPairStatus ToIndividualPairStatus(this UserInfo userInfo)
    {
        if (userInfo.IndividuallyPaired) return IndividualPairStatus.Bidirectional;
        if (!userInfo.IndividuallyPaired && userInfo.GIDs.Contains("//GAGSPEAK//DIRECT", StringComparer.Ordinal)) return IndividualPairStatus.OneSided;
        return IndividualPairStatus.None;
    }

    private static long _noIpCntr = 0;

    /// <summary> Get the IP address of the client </summary>
    public static string GetIpAddress(this IHttpContextAccessor accessor)
    {
        // Try to get the IP address from the Cloudflare header
        try
        {
            // if the accessor's HttpContext.Request.Headers["CF-CONNECTING-IP"] is not null or empty, return the value of the header
            if (!string.IsNullOrEmpty(accessor.HttpContext.Request.Headers["CF-CONNECTING-IP"]))
                return accessor.HttpContext.Request.Headers["CF-CONNECTING-IP"];

            // if the accessor's HttpContext.Request.Headers["X-Forwarded-For"] is not null or empty, return the value of the header
            if (!string.IsNullOrEmpty(accessor.HttpContext.Request.Headers["X-Forwarded-For"]))
            {
                return accessor.HttpContext.Request.Headers["X-Forwarded-For"];
            }

            // define the ip address as the accessor's HttpContext.Connection.RemoteIpAddress.ToString() or "NoIp" + the noIpCntr
            var ipAddress = accessor.HttpContext.GetServerVariable("HTTP_X_FORWARDED_FOR");

            // if the ip address is not null or whitespace
            if (!string.IsNullOrWhiteSpace(ipAddress))
            {
                // split the ip address by commas and remove any empty entries
                var addresses = ipAddress.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var lastEntry = addresses.LastOrDefault();
                if (lastEntry != null)
                {
                    return lastEntry;
                }
            }

            // return the ip address or "NoIp" + the noIpCntr
            return accessor.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "NoIp";
        }
        // if an exception is caught, return "NoIp" + the noIpCntr
        catch
        {
            return "NoIp" + _noIpCntr++;
        }
    }
}