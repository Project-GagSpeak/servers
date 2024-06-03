using GagspeakServer.Models;
using Microsoft.EntityFrameworkCore;
using GagspeakServer.Utils;
using Microsoft.IdentityModel.Tokens;
using Gagspeak.API.Data;
using Microsoft.AspNetCore.SignalR;
using GagspeakServer.Data;

namespace GagspeakServer.Hubs;

public partial class GagspeakHub
{
    public string UserCharaIdent 
        => Context.User?.Claims?.SingleOrDefault(c => string.Equals(c.Type, GagspeakClaimTypes.CharaIdent, StringComparison.Ordinal))?.Value
        ?? throw new Exception("No Chara Ident in Claims");

    public string UserUID
        => Context.User?.Claims?.SingleOrDefault(c => string.Equals(c.Type, GagspeakClaimTypes.Uid, StringComparison.Ordinal))?.Value
        ?? throw new Exception("No UID in Claims");

    public string Continent
        => Context.User?.Claims?.SingleOrDefault(c => string.Equals(c.Type, GagspeakClaimTypes.Continent, StringComparison.Ordinal))?.Value 
        ?? "UNK";
}