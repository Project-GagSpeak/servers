using GagspeakShared.Data;
using GagspeakShared.Metrics;
using GagspeakShared.Models;
using GagspeakShared.Services;
using GagspeakShared.Utils.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace GagspeakAuthentication.Services;
#pragma warning disable CS8604 // Possible nulls for auth replies should be valid. (change if they shouldnt)

/// <summary> Authenticator Service for our secret key authentication system. </summary>
public class SecretKeyAuthenticatorService
{
    private readonly GagspeakMetrics _metrics;
    private readonly IDbContextFactory<GagspeakDbContext> _dbContextFactory;
    private readonly IConfigurationService<AuthServiceConfiguration> _configurationService;
    private readonly ILogger<SecretKeyAuthenticatorService> _logger;
    private readonly ConcurrentDictionary<string, SecretKeyFailedAuthorization> _failedAuthorizations = new(StringComparer.Ordinal);

    public SecretKeyAuthenticatorService(GagspeakMetrics metrics, IDbContextFactory<GagspeakDbContext> dbContextFactory,
        IConfigurationService<AuthServiceConfiguration> configuration, ILogger<SecretKeyAuthenticatorService> logger)
    {
        _logger = logger;
        _configurationService = configuration;
        _metrics = metrics;
        _dbContextFactory = dbContextFactory;
    }

    /// <summary> Authorize a user with a secret key. </summary>
    /// <param name="ip"> The IP address of the user. (i think we used the geoIPservice just to help make sure people dont spam authentications </param>
    /// <param name="hashedSecretKey"> The hashed secret key of the user. </param>
    /// <returns> The secret key authorization reply. </returns>
    public async Task<SecretKeyAuthReply> AuthorizeAsync(string ip, string hashedSecretKey)
    {
        // increase the counter of our metrics for the authentication requests.
        _metrics.IncCounter(MetricsAPI.CounterAuthenticationRequests);

        // if the ip is in the failed authorizations list, and the failed attempts > failed auth for temp ban, then temp ban the IP
        if (_failedAuthorizations.TryGetValue(ip, out var existingFailedAuthorization)
        && existingFailedAuthorization.FailedAttempts > _configurationService.GetValueOrDefault(nameof(AuthServiceConfiguration.FailedAuthForTempBan), 5))
        {
            // and if the reset task is null, then we can temp ban the IP.
            if (existingFailedAuthorization.ResetTask is null)
            {
                // log the temp ban
                _logger.LogWarning("TempBan {ip} for authorization spam", ip);
                // set the reset task to a new task that will run after the temp ban duration.
                existingFailedAuthorization.ResetTask = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromMinutes(_configurationService.GetValueOrDefault(nameof(AuthServiceConfiguration.TempBanDurationInMinutes), 5))).ConfigureAwait(false);

                }).ContinueWith((t) => // then when the task is done, remove the IP from the failed authorizations list.
                {
                    _failedAuthorizations.Remove(ip, out _);
                });
            }
#pragma warning disable CS8625
            return new(Success: false, Uid: null, PrimaryUid: null, Alias: null, TempBan: true, Permaban: false);
#pragma warning restore CS8625
        }

        // grab the context from the db factory
        using var context = await _dbContextFactory.CreateDbContextAsync().ConfigureAwait(false);
        // get the auth reply from the context, including the user, from the Auth table where the hashed key is the same as the hashed secret key.
        Auth? authReply = await context.Auth.Include(a => a.User).AsNoTracking()
            .SingleOrDefaultAsync(u => u.HashedKey == hashedSecretKey).ConfigureAwait(false);
        // set the isbanned variable to the authreply objects isbanned variable. Set to false if null.
        bool isBanned = authReply?.IsBanned ?? false;
        // get the primary user UID string from the auth reply, or the user UID if the primary user UID is null.
        var primaryUid = authReply?.PrimaryUserUID ?? authReply?.UserUID;

        // if our authreply does have the primary user ID as not null, then we need to check if the primary user is banned.
        if (authReply?.PrimaryUserUID != null)
        {
            // get the primary user from the context, where the user UID is the primary user UID from the auth reply.
            var primaryUser = await context.Auth.AsNoTracking().SingleOrDefaultAsync(u => u.UserUID == authReply.PrimaryUserUID).ConfigureAwait(false);
            // see if the primary user is banned, and set the isbanned variable to true if they are.
            isBanned = isBanned || (primaryUser?.IsBanned ?? false);
        }

        // create a new secret key auth replace object, 
        SecretKeyAuthReply reply = new(
            authReply != null,                              // set the boolean sueccess to true if the auth reply is not null.
            authReply?.UserUID,                             // set the UID to the auth reply user UID.
            authReply?.PrimaryUserUID ?? authReply?.UserUID,// set the primary UID to the primary user UID, or the user UID if the primary UID is null.
            authReply?.User?.Alias ?? string.Empty,         // set the alias to the user alias, or an empty string if the user is null.
            TempBan: false,                                 // set the temp ban to false.
            isBanned                                        // set the permaban to the isbanned variable.
        );

        // if the reply was a success, then increase the success counter for the number of authentication successes and the number of cache entries.
        if (reply.Success)
        {
            _metrics.IncCounter(MetricsAPI.CounterAuthenticationSuccess);
        }
        else
        {
            // otherwise, return an authentication failure.
            return AuthenticationFailure(ip);
        }

        return reply;
    }

    /// <summary> Handler for an authentication failure. </summary>
    /// <param name="ip">the ip address of the request that just had the failure.</param>
    /// <returns> The secret key auth reply for the failed authentication. </returns>
    private SecretKeyAuthReply AuthenticationFailure(string ip)
    {
        // increase the counter for the number of authentication failures.
        _metrics.IncCounter(MetricsAPI.CounterAuthenticationFailed);

        // log the failed authorization from the IP.
        _logger.LogWarning("Failed authorization from {ip}", ip);
        // set the whitelisted variable to the whitelisted IPs from the configuration service.
        var whitelisted = _configurationService.GetValueOrDefault(nameof(AuthServiceConfiguration.WhitelistedIps), new List<string>());

        // if the IP does not exist in the list of whitelisted IPs, then increase the failed attempts for the IP.
        if (!whitelisted.Exists(w => ip.Contains(w, StringComparison.OrdinalIgnoreCase)))
        {
            // if the IP is in the failed authorizations list, then increase the failed attempts for the IP.
            if (_failedAuthorizations.TryGetValue(ip, out var auth))
            {
                auth.IncreaseFailedAttempts();
            }
            else
            {
                // otherwise, add the IP to the failed authorizations list.
                _failedAuthorizations[ip] = new SecretKeyFailedAuthorization();
            }
        }

        // return the failed secretkeyauthreply object.
#pragma warning disable CS8625
        return new(Success: false, Uid: null, PrimaryUid: null, Alias: null, TempBan: false, Permaban: false);
#pragma warning restore CS8625
    }
}
#pragma warning restore CS8604