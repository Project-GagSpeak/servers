using Gagspeak.API.Data.Enum;
using Gagspeak.API.SignalR;
using GagSpeak.API.Dto.Connection;
using GagspeakServer.Services;
using GagspeakServer.Utils;
using GagspeakShared.Data;
using GagspeakShared.Metrics;
using GagspeakShared.Models;
using GagspeakShared.Services;
using GagspeakShared.Utils;
using GagspeakShared.Utils.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System.Collections.Concurrent;

namespace GagspeakServer.Hubs;


/// <summary>
/// Use the same claim types as the gagspeak hub to make things easier for reconnecting clients
/// </summary>
public partial class ToyboxHub : Hub<IToyboxHub>, IToyboxHub
{
    public string UserCharaIdent => Context.User?.Claims?.SingleOrDefault(c => string.Equals(c.Type, GagspeakClaimTypes.CharaIdent, StringComparison.Ordinal))?.Value ?? throw new Exception("No Chara Ident in Claims");
    public string UserUID => Context.User?.Claims?.SingleOrDefault(c => string.Equals(c.Type, GagspeakClaimTypes.Uid, StringComparison.Ordinal))?.Value ?? throw new Exception("No UID in Claims");
    public string UserHasTempAccess => Context.User?.Claims?.SingleOrDefault(c => string.Equals(c.Type, GagspeakClaimTypes.AccessType, StringComparison.Ordinal))?.Value ?? throw new Exception("No TempAccess in Claims");


    /// <summary> Helper function to remove a user from the redis by their UID</summary>
    private async Task RemoveUserFromRedis()
    {
        await _redis.RemoveAsync("ToyboxHub:UID:" + UserUID, StackExchange.Redis.CommandFlags.FireAndForget).ConfigureAwait(false);
    }

    /// <summary> A helper function to update the user's identity on the redi's by their UID</summary>
    private async Task UpdateUserOnRedis()
    {
        await _redis.AddAsync("ToyboxHub:UID:" + UserUID, UserCharaIdent, TimeSpan.FromSeconds(60), StackExchange.Redis.When.Always, StackExchange.Redis.CommandFlags.FireAndForget).ConfigureAwait(false);
    }
}

