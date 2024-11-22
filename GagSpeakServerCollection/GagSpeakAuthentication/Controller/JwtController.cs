using GagspeakAPI.Routes;
using GagspeakAuthentication.Services;
using GagspeakShared.Data;
using GagspeakShared.Models;
using GagspeakShared.Services;
using GagspeakShared.Utils;
using GagspeakShared.Utils.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis.Extensions.Core.Abstractions;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GagspeakAuthentication.Controllers;

[AllowAnonymous]
[Route(GagspeakAuth.Auth)]
public class JwtController : Controller
{
    private readonly ILogger<JwtController> _logger;
    private readonly IHttpContextAccessor _accessor;
    private readonly IConfigurationService<AuthServiceConfiguration> _configuration;
    private readonly GagspeakDbContext _gagspeakDbContext;
    private readonly IRedisDatabase _redis;
    //private readonly GeoIPService _geoIPProvider; ((would really love to avoid tracking peoples location information if possible lol))
    private readonly SecretKeyAuthenticatorService _secretKeyAuthenticatorService;

    public JwtController(ILogger<JwtController> logger,
        IHttpContextAccessor accessor, GagspeakDbContext gagspeakDbContext,
        SecretKeyAuthenticatorService secretKeyAuthenticatorService,
        IConfigurationService<AuthServiceConfiguration> configuration,
        IRedisDatabase redisDb)
    {
        // Initialize private fields with the provided services
        _logger = logger;
        _accessor = accessor;
        _redis = redisDb;
        _gagspeakDbContext = gagspeakDbContext;
        _secretKeyAuthenticatorService = secretKeyAuthenticatorService;
        _configuration = configuration;
    }

    /// <summary>
    /// Allows us to make use of the localcontentID over the auth hashed key requirement to create a temporary access.
    /// </summary>
    /// <param name="localContentID"> PlayerCharactrer's localContentID </param>
    /// <param name="charaIdent"> PlayerCharacter's identifier </param>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpPost(GagspeakAuth.Auth_TempToken)]
    public async Task<IActionResult> CreateTemporaryToken(string localContentID, string charaIdent)
    {
        _logger.LogInformation("CreateTemporaryToken:SUCCESS:{ident}", charaIdent);
        // Call internal authentication method
        return await AuthenticateInternal(null, charaIdent, localContentID).ConfigureAwait(false);
    }



    /// <summary> The method to create a new token for a user. (Allows Anonymous access)
    /// <para>
    /// Method is not called upon directly from the code, 
    /// but rather from a HTTPpost under the [StringSyntax("Route")] 
    /// path defined in GagspeakAuth from the API
    /// </para>
    /// </summary>
    /// <param name="auth">the authentication string</param>
    /// <param name="charaIdent">the indentity of the character to make a token for</param>
    /// <returns> A task that represents the asynchronous operation. The task result contains an IActionResult. </returns>
    [AllowAnonymous]
    [HttpPost(GagspeakAuth.Auth_CreateIdent)]
    public async Task<IActionResult> CreateToken(string auth, string charaIdent)
    {
        // Call internal authentication method
        return await AuthenticateInternal(auth, charaIdent).ConfigureAwait(false);
    }

    /// <summary> The method to renew a token for a user. (Requires Authenticated access)
    /// <para>
    /// Method is not called upon directly from the code,
    /// but rather from a HTTPget under the [StringSyntax("Route")]
    /// path defined in GagspeakAuth from the API
    /// </para>
    /// </summary>
    /// <returns> A task that represents the asynchronous operation. The task result contains an IActionResult. </returns>
    [Authorize(Policy = "Authenticated")]
    [HttpGet("renewToken")]
    public async Task<IActionResult> RenewToken()
    {
        try
        {
            // Extract user claims for UID, CharaIdentity, and Alias from the HTTPContext claims.
            var uid = HttpContext.User.Claims.Single(p => string.Equals(p.Type, GagspeakClaimTypes.Uid, StringComparison.Ordinal))!.Value;
            var ident = HttpContext.User.Claims.Single(p => string.Equals(p.Type, GagspeakClaimTypes.CharaIdent, StringComparison.Ordinal))!.Value;
            var alias = HttpContext.User.Claims.SingleOrDefault(p => string.Equals(p.Type, GagspeakClaimTypes.Alias))?.Value ?? string.Empty;

            // Check if the user is banned from the gagspeak servers.
            if (await _gagspeakDbContext.Auth.Where(u => u.UserUID == uid || u.PrimaryUserUID == uid).AnyAsync(a => a.IsBanned))
            {
                // Ensure the user is banned
                await EnsureBan(uid, ident);
                // return that they are unauthorized
                return Unauthorized("You are permanently banned.");
            }

            // check if the user's identity is banned from using the service.
            if (await IsIdentBanned(uid, ident))
            {
                // Log the ban and return an unauthorized result
                return Unauthorized("Your character is banned from using the service.");
            }

            // they are not banned, so log the sucess and await the creation of a jwt from an ID.
            _logger.LogInformation("RenewToken:SUCCESS:{id}:{ident}", uid, ident);
            return await CreateJwtFromId(uid, ident, alias);
        }
        catch (Exception ex)
        {
            // Log the error and return an Unauthorized result if we catch an exception.
            _logger.LogError(ex, "RenewToken:FAILURE");
            return Unauthorized("Unknown error while renewing authentication token");
        }
    }

    /// <summary> Method to internally authenticate a user.
    /// <para> Must known the authentication string and the user's identity</para>
    /// </summary>
    /// <param name="auth"> secret key authentication </param>
    /// <param name="charaIdent"> character identifier </param>
    /// <returns> A task that represents the asynchronous operation. The task result contains an IActionResult. </returns>
    private async Task<IActionResult> AuthenticateInternal(string auth, string charaIdent, string? localContentID = null)
    {
        try
        {
            _logger.LogInformation("Authenticating {ident}", charaIdent);
            // required for both token types, so check if the authentication string or character identity is empty or null.
            if (string.IsNullOrEmpty(charaIdent)) return BadRequest("No CharaIdent");

            // handle localcontentID based authentication
            if (!string.IsNullOrEmpty(localContentID))
            {
                // validate the localcontentID here (e.g., check if it exists in your database)
                // if validation fails, return appropriate responce
                if (localContentID == null) // replace with better security
                {
                    _logger.LogInformation("Authenticate:LOCALCONTENTID:{id}:{ident}", localContentID, charaIdent);
                    return BadRequest("Invalid LocalContentID");
                }

                // assuming the local coneent ID is valid, create a token with the TemporaryAccess claim
                return await CreateTempAccessJwtFromId(localContentID, charaIdent);                                       // WE CREATE THE JWT HERE
            }

            // check to see if the secret key is empty or null, and return a bad request if it is.
            if (string.IsNullOrEmpty(auth)) return BadRequest("No Authkey");

            // the passed in variables had content, so fetch the IPGetIpAddress
            var ip = _accessor.GetIpAddress();

            _logger.LogInformation("Attempting to authenticate secret key {auth}", auth);
            SecretKeyAuthReply authResult = await _secretKeyAuthenticatorService.AuthorizeAsync(ip, auth);
            // see if the authorize was valid
            _logger.LogDebug("AuthResult: {authResult}", authResult);

            // if the ident (identifier) is banned, return an unauthorized result.
            if (await IsIdentBanned(authResult.Uid, charaIdent))
            {
                _logger.LogWarning("Authenticate:IDENTBAN:{id}:{ident}", authResult.Uid, charaIdent);
                return Unauthorized("Your character is banned from using the service.");
            }

            // if the result was not successful and the user is not temporarily banned,
            if (!authResult.Success && !authResult.TempBan)
            {
                _logger.LogWarning("Authenticate:INVALID:{id}:{ident}", authResult?.Uid ?? "NOUID", charaIdent);
                return Unauthorized("The provided secret key is invalid. Verify your accounts existence and/or recover the secret key.");
            }

            // if the result was not successful and the user is temporarily banned,
            if (!authResult.Success && authResult.TempBan)
            {
                _logger.LogWarning("Authenticate:TEMPBAN:{id}:{ident}", authResult.Uid ?? "NOUID", charaIdent);
                return Unauthorized("Due to an excessive amount of failed authentication attempts you are temporarily banned. Check your Secret Key configuration and try connecting again in 5 minutes.");
            }

            // if the user is permanently banned, ensure the ban and return an unauthorized result.
            if (authResult.Permaban)
            {
                // sure ban before returning the unauthorized result.
                await EnsureBan(authResult.Uid, charaIdent);

                _logger.LogWarning("Authenticate:UIDBAN:{id}:{ident}", authResult.Uid, charaIdent);
                return Unauthorized("You are permanently banned.");
            }

            // see if the user is already currently logged in with the same identifier.
            var existingIdent = await _redis.GetAsync<string>("GagspeakHub:UID:" + authResult.Uid);
            // if they are, return an unauthorized result. (already logged in)
            if (!string.IsNullOrEmpty(existingIdent))
            {
                _logger.LogWarning("Authenticate:DUPLICATE:{id}:{ident}", authResult.Uid, charaIdent);
                return Unauthorized("Already logged in to this account. Reconnect in 60 seconds. If you keep seeing this issue, restart your game.");
            }

            // if the user is not already logged in, set the redis key to the identifier.
            _logger.LogInformation("Authenticate:SUCCESS:{id}:{ident}", authResult.Uid, charaIdent);

            // finally, create a jwt from the user's ID and character identity.
            return await CreateJwtFromId(authResult.Uid, charaIdent, authResult.Alias ?? string.Empty);     // we create the jwt here
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Authenticate:UNKNOWN");
            return Unauthorized("Unknown internal server error during authentication");
        }
    }

    /// <summary> Method to create a JWT token from a provided user ID, character identity, and alias. </summary>
    private async Task<IActionResult> CreateTempAccessJwtFromId(string charaIdent, string localConentId)
    {
        var token = CreateJwt(new List<Claim>
        {
            new Claim(GagspeakClaimTypes.CharaIdent, charaIdent),
            new Claim(GagspeakClaimTypes.Expires, DateTime.UtcNow.AddHours(6).Ticks.ToString(CultureInfo.InvariantCulture)), // the expiration claim
            new Claim(GagspeakClaimTypes.AccessType, "LocalContent")
        });
        // return the token as a content result from the rawdata.
        return Content(token.RawData);
    }

    /// <summary> Method to create a JWT token from a provided user ID, character identity, and alias. </summary>
    private async Task<IActionResult> CreateJwtFromId(string uid, string charaIdent, string alias)
    {
        // create a new token from the provided claims.
        var token = CreateJwt(new List<Claim>()
        {
            new Claim(GagspeakClaimTypes.Uid, uid),                 // the UID claim
            new Claim(GagspeakClaimTypes.CharaIdent, charaIdent),   // the character identifier claim
            new Claim(GagspeakClaimTypes.Alias, alias),             // the UID alias claim
            new Claim(GagspeakClaimTypes.Expires, DateTime.UtcNow.AddHours(6).Ticks.ToString(CultureInfo.InvariantCulture)), // the expiration claim
            new Claim(GagspeakClaimTypes.AccessType, "SecretKey")
        });

        // return the token as a content result from the rawdata.
        return Content(token.RawData);
    }

    /// <summary> Method to create a JWT token from a provided set of claims. </summary>
    private JwtSecurityToken CreateJwt(IEnumerable<Claim> authClaims)
    {
        // create the authentication signing key using the configuration value for the JWT.
        var authSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_configuration.GetValue<string>(nameof(GagspeakConfigurationBase.Jwt))));

        // generate the token via a new securityTokenDescriptor:
        var token = new SecurityTokenDescriptor()
        {
            // set the subject to the authclaims provided.
            Subject = new ClaimsIdentity(authClaims),
            // set the signing credentials to the auth signing key.
            SigningCredentials = new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256Signature),
            // set the expiration date to the provided expiration date by the authclaims.
            Expires = new(long.Parse(authClaims.First(f => string.Equals(f.Type, GagspeakClaimTypes.Expires, StringComparison.Ordinal)).Value!, CultureInfo.InvariantCulture), DateTimeKind.Utc),
        };

        // set the handler to the new JWT security token handler and create the token.
        var handler = new JwtSecurityTokenHandler();
        // create a new jwt securitytoken from the token generated, and return that.
        return handler.CreateJwtSecurityToken(token);
    }

    /// <summary> Helper method to ensure a user UID and identifier is banned from the service. </summary>
    /// <param name="uid">the user UID that we want to ensure is banned</param>
    /// <param name="charaIdent"> the character identifier that we want to ensure is banned</param>
    private async Task EnsureBan(string uid, string charaIdent)
    {
        // if the character identifier is not already banned,
        if (!_gagspeakDbContext.BannedUsers.Any(c => c.CharacterIdentification == charaIdent))
        {
            // add the banned user to the database by adding it to the bannedUsers table.
            _gagspeakDbContext.BannedUsers.Add(new Banned()
            {
                CharacterIdentification = charaIdent,
                UserUID = uid,
                Reason = "Autobanned CharacterIdent (" + uid + ")",
            });

            // save the gagspeak database changes.
            await _gagspeakDbContext.SaveChangesAsync();
        }

        // fetch the primary user from the auth table where the primary user UID is the same as the user UID.
        var primaryUser = await _gagspeakDbContext.Auth.Include(a => a.User).FirstOrDefaultAsync(f => f.PrimaryUserUID == uid);
        // set the toBanUID to the primary user UID if the primary user is not null, otherwise set it to the user UID.
        var toBanUid = primaryUser == null ? uid : primaryUser.UserUID;

        // fetch the accountClaimAuth used to claim ownership over the account, if one exists.
        var accountClaimAuth = await _gagspeakDbContext.AccountClaimAuth.Include(a => a.User).FirstOrDefaultAsync(c => c.User.UID == toBanUid);

        // if it does exist
        if (accountClaimAuth != null)
        {
            if (!_gagspeakDbContext.BannedRegistrations.Any(c => c.DiscordId == accountClaimAuth.DiscordId.ToString()))
            {
                // if it exists, then add the banned registration to the database.
                _gagspeakDbContext.BannedRegistrations.Add(new BannedRegistrations()
                {
                    // set the discord ID to the accountClaimAuth discord ID.
                    DiscordId = accountClaimAuth.DiscordId.ToString(),
                });
            }
            // save the changes to the gagspeak database.
            await _gagspeakDbContext.SaveChangesAsync();
        }
    }

    /// <summary> Helper func to see if the character identifier is banned from the service. </summary>
    /// <param name="uid">the user UID</param>
    /// <param name="charaIdent">the user identifier</param>
    /// <returns> A task that represents the asynchronous operation. The task result contains a boolean. </returns>
    private async Task<bool> IsIdentBanned(string uid, string charaIdent)
    {
        // see if user is in banned users table where the charaIdentis the same as the character identifier.
        var isBanned = await _gagspeakDbContext.BannedUsers.AsNoTracking().AnyAsync(u => u.CharacterIdentification == charaIdent).ConfigureAwait(false);

        // if they are
        if (isBanned)
        {
            // fetch the authentication object as the row found in the database where the user UID matches.
            Auth? authToBan = _gagspeakDbContext.Auth.SingleOrDefault(a => a.UserUID == uid);

            // if it isnt null
            if (authToBan != null)
            {
                // set IsBanned to true and save the changes to the database.
                authToBan.IsBanned = true;
                await _gagspeakDbContext.SaveChangesAsync().ConfigureAwait(false);
            }
        }

        // return if they were banned.
        return isBanned;
    }
}