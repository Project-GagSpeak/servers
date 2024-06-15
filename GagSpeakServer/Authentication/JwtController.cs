using Gagspeak.API.Routes;
using GagspeakServer;
using GagspeakServer.Data;
using GagspeakServer.Models;
using GagspeakServer.Services;
using GagspeakServer.Utils;
using GagspeakServer.Utils.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GagspeakServer.Authentication;

[AllowAnonymous]
[Route(GagspeakAuth.Auth)]
public class JwtController : Controller
{
    private readonly ILogger<JwtController> _logger;
    private readonly IHttpContextAccessor _accessor;
    private readonly IConfigurationService<AuthServiceConfiguration> _configuration;
    private readonly GagspeakDbContext _gagspeakDbContext;
    private readonly IRedisDatabase _redis;
    private readonly GeoIPService _geoIPProvider;
    private readonly SecretKeyAuthenticatorService _secretKeyAuthenticatorService;
// Constructor for JwtController
public JwtController(ILogger<JwtController> logger,
        IHttpContextAccessor accessor, GagspeakDbContext gagspeakDbContext,
        SecretKeyAuthenticatorService secretKeyAuthenticatorService,
        IConfigurationService<AuthServiceConfiguration> configuration,
        IRedisDatabase redisDb, GeoIPService geoIPProvider)
    {
        // Initialize private fields with the provided services
        _logger = logger;
        _accessor = accessor;
        _redis = redisDb;
        _geoIPProvider = geoIPProvider;
        _gagspeakDbContext = gagspeakDbContext;
        _secretKeyAuthenticatorService = secretKeyAuthenticatorService;
        _configuration = configuration;
    }

    // Endpoint to create a new token
    [AllowAnonymous]
    [HttpPost(GagspeakAuth.Auth_CreateIdent)]
    public async Task<IActionResult> CreateToken(string auth, string charaIdent)
    {
        // Call internal authentication method
        return await AuthenticateInternal(auth, charaIdent).ConfigureAwait(false);
    }

    // Endpoint to renew an existing token
    [Authorize(Policy = "Authenticated")]
    [HttpGet("renewToken")]
    public async Task<IActionResult> RenewToken()
    {
        try
        {
            // Extract user claims from the HttpContext
            var uid = HttpContext.User.Claims.Single(p => string.Equals(p.Type, GagspeakClaimTypes.Uid, StringComparison.Ordinal))!.Value;
            var ident = HttpContext.User.Claims.Single(p => string.Equals(p.Type, GagspeakClaimTypes.CharaIdent, StringComparison.Ordinal))!.Value;
            var alias = HttpContext.User.Claims.SingleOrDefault(p => string.Equals(p.Type, GagspeakClaimTypes.Alias))?.Value ?? string.Empty;

            // Check if the user is banned
            if (await _gagspeakDbContext.Auth.Where(u => u.UserUID == uid || u.PrimaryUserUID == uid).AnyAsync(a => a.IsBanned))
            {
                await EnsureBan(uid, ident);

                return Unauthorized("You are permanently banned.");
            }

            // Check if the character is banned
            if (await IsIdentBanned(uid, ident))
            {
                return Unauthorized("Your character is banned from using the service.");
            }

            // Log the successful token renewal
            _logger.LogInformation("RenewToken:SUCCESS:{id}:{ident}", uid, ident);
            return await CreateJwtFromId(uid, ident, alias);
        }
        catch (Exception ex)
        {
            // Log the error and return an Unauthorized result
            _logger.LogError(ex, "RenewToken:FAILURE");
            return Unauthorized("Unknown error while renewing authentication token");
        }
    }

    private async Task<IActionResult> AuthenticateInternal(string auth, string charaIdent)
    {
        try
        {
            if (string.IsNullOrEmpty(auth)) return BadRequest("No Authkey");
            if (string.IsNullOrEmpty(charaIdent)) return BadRequest("No CharaIdent");

            var ip = _accessor.GetIpAddress();

            var authResult = await _secretKeyAuthenticatorService.AuthorizeAsync(ip, auth);

            if (await IsIdentBanned(authResult.Uid, charaIdent))
            {
                _logger.LogWarning("Authenticate:IDENTBAN:{id}:{ident}", authResult.Uid, charaIdent);
                return Unauthorized("Your character is banned from using the service.");
            }

            if (!authResult.Success && !authResult.TempBan)
            {
                _logger.LogWarning("Authenticate:INVALID:{id}:{ident}", authResult?.Uid ?? "NOUID", charaIdent);
                return Unauthorized("The provided secret key is invalid. Verify your accounts existence and/or recover the secret key.");
            }
            if (!authResult.Success && authResult.TempBan)
            {
                _logger.LogWarning("Authenticate:TEMPBAN:{id}:{ident}", authResult.Uid ?? "NOUID", charaIdent);
                return Unauthorized("Due to an excessive amount of failed authentication attempts you are temporarily banned. Check your Secret Key configuration and try connecting again in 5 minutes.");
            }
            if (authResult.Permaban)
            {
                await EnsureBan(authResult.Uid, charaIdent);

                _logger.LogWarning("Authenticate:UIDBAN:{id}:{ident}", authResult.Uid, charaIdent);
                return Unauthorized("You are permanently banned.");
            }

            var existingIdent = await _redis.GetAsync<string>("UID:" + authResult.Uid);
            if (!string.IsNullOrEmpty(existingIdent))
            {
                _logger.LogWarning("Authenticate:DUPLICATE:{id}:{ident}", authResult.Uid, charaIdent);
                return Unauthorized("Already logged in to this account. Reconnect in 60 seconds. If you keep seeing this issue, restart your game.");
            }

            _logger.LogInformation("Authenticate:SUCCESS:{id}:{ident}", authResult.Uid, charaIdent);
            return await CreateJwtFromId(authResult.Uid, charaIdent, authResult.Alias ?? string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Authenticate:UNKNOWN");
            return Unauthorized("Unknown internal server error during authentication");
        }
    }

    private JwtSecurityToken CreateJwt(IEnumerable<Claim> authClaims)
    {
        var authSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_configuration.GetValue<string>(nameof(GagspeakConfigurationBase.Jwt))));

        var token = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(authClaims),
            SigningCredentials = new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256Signature),
            Expires = new(long.Parse(authClaims.First(f => string.Equals(f.Type, GagspeakClaimTypes.Expires, StringComparison.Ordinal)).Value!, CultureInfo.InvariantCulture), DateTimeKind.Utc),
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.CreateJwtSecurityToken(token);
    }

    private async Task<IActionResult> CreateJwtFromId(string uid, string charaIdent, string alias)
    {
        var token = CreateJwt(new List<Claim>()
        {
            new Claim(GagspeakClaimTypes.Uid, uid),
            new Claim(GagspeakClaimTypes.CharaIdent, charaIdent),
            new Claim(GagspeakClaimTypes.Alias, alias),
            new Claim(GagspeakClaimTypes.Expires, DateTime.UtcNow.AddHours(6).Ticks.ToString(CultureInfo.InvariantCulture)),
            new Claim(GagspeakClaimTypes.Continent, await _geoIPProvider.GetCountryFromIP(_accessor))
        });

        return Content(token.RawData);
    }

    private async Task EnsureBan(string uid, string charaIdent)
    {
        if (!_gagspeakDbContext.BannedUsers.Any(c => c.CharacterIdentification == charaIdent))
        {
            _gagspeakDbContext.BannedUsers.Add(new Banned()
            {
                CharacterIdentification = charaIdent,
                Reason = "Autobanned CharacterIdent (" + uid + ")",
            });

            await _gagspeakDbContext.SaveChangesAsync();
        }

        var primaryUser = await _gagspeakDbContext.Auth.Include(a => a.User).FirstOrDefaultAsync(f => f.PrimaryUserUID == uid);

        var toBanUid = primaryUser == null ? uid : primaryUser.UserUID;

        var lodestone = await _gagspeakDbContext.LodeStoneAuth.Include(a => a.User).FirstOrDefaultAsync(c => c.User.UID == toBanUid);

        if (lodestone != null)
        {
            if (!_gagspeakDbContext.BannedRegistrations.Any(c => c.DiscordIdOrLodestoneAuth == lodestone.HashedLodestoneId))
            {
                _gagspeakDbContext.BannedRegistrations.Add(new BannedRegistrations()
                {
                    DiscordIdOrLodestoneAuth = lodestone.HashedLodestoneId,
                });
            }
            if (!_gagspeakDbContext.BannedRegistrations.Any(c => c.DiscordIdOrLodestoneAuth == lodestone.DiscordId.ToString()))
            {
                _gagspeakDbContext.BannedRegistrations.Add(new BannedRegistrations()
                {
                    DiscordIdOrLodestoneAuth = lodestone.DiscordId.ToString(),
                });
            }

            await _gagspeakDbContext.SaveChangesAsync();
        }
    }

    private async Task<bool> IsIdentBanned(string uid, string charaIdent)
    {
        var isBanned = await _gagspeakDbContext.BannedUsers.AsNoTracking().AnyAsync(u => u.CharacterIdentification == charaIdent).ConfigureAwait(false);
        if (isBanned)
        {
            var authToBan = _gagspeakDbContext.Auth.SingleOrDefault(a => a.UserUID == uid);
            if (authToBan != null)
            {
                authToBan.IsBanned = true;
                await _gagspeakDbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        return isBanned;
    }
}