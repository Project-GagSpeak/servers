using Gagspeak.API.SignalR;
using GagspeakServer.Hubs;
using GagspeakShared.Data;
using GagspeakShared.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;

namespace GagspeakServer.Listeners;
public class DbNotificationListener : IHostedService
{
    private readonly IDbContextFactory<GagspeakDbContext> _dbContextFactory;
    private readonly IHubContext<GagspeakHub, IGagspeakHub> _hubContext;
    private readonly ILogger<DbNotificationListener> _logger;
    private readonly string _connectionString;
    private Task _listeningTask;
    private CancellationTokenSource _stoppingCts;

    public DbNotificationListener(ILogger<DbNotificationListener> logger, 
        IDbContextFactory<GagspeakDbContext> GagSpeakDbContextFactory,
        IHubContext<GagspeakHub, IGagspeakHub> hubContext, IConfiguration configuration)
    {
        _dbContextFactory = GagSpeakDbContextFactory;
        _hubContext = hubContext;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listeningTask = Task.Run(ListenForNotificationsAsync, _stoppingCts.Token);
        return Task.CompletedTask;
    }

    private async Task ListenForNotificationsAsync()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(_stoppingCts.Token);
        await using (var cmd = new NpgsqlCommand("LISTEN accountclaimauth_insert", connection))
        {
            cmd.ExecuteNonQuery();
        }

        connection.Notification += async (o, e) =>
        {
            _logger.LogInformation($"[[ Notification received]] : {e.Payload}");
            try
            {
                var accountClaimAuth = ParsePayloadToAccountClaimAuth(e.Payload);
                var accountClaims = new List<AccountClaimAuth> { accountClaimAuth };
                // dont allow any entries in with empty initial keys or if the size is 0
                if (string.IsNullOrEmpty(accountClaimAuth.InitialGeneratedKey) || accountClaimAuth.InitialGeneratedKey.Length == 0)
                {
                    _logger.LogError($"[[ Notification error]] : InitialGeneratedKey is empty or null");
                    return;
                }
                _logger.LogInformation($"[[ Notification parsed]] : {accountClaims.Count} account claims received");

                // execute the logic
                await using var dbContext = _dbContextFactory.CreateDbContext();

                _logger.LogInformation("Displaying verification codes to users");

                // for each authentication that was newly added
                foreach (AccountClaimAuth auth in accountClaims)
                {
                    // locate the auth in the database with the matching hashed key
                    var matchingUserAuth = await dbContext.Auth.AsNoTracking().SingleAsync(u => u.HashedKey == auth.InitialGeneratedKey).ConfigureAwait(false);

                    // then locate the userUID of that auth object
                    var userUID = matchingUserAuth.UserUID;

                    // see if that user UID is in the list of user connections
                    if (true /*_userConnections.ContainsKey(userUID)*/)
                    {
                        // if it is, send the verification code to the user
                        await _hubContext.Clients.User(userUID).Client_DisplayVerificationPopup(new() { VerificationCode = auth.VerificationCode ?? "" }).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing notification: {ex.Message}");
            }
        };

        while (!_stoppingCts.Token.IsCancellationRequested)
        {
            await connection.WaitAsync(_stoppingCts.Token);
        }
    }

    public AccountClaimAuth ParsePayloadToAccountClaimAuth(string jsonPayload)
    {
        var jObject = JObject.Parse(jsonPayload);

        var accountClaimAuth = new AccountClaimAuth
        {
            DiscordId = (ulong)jObject["discord_id"],
            InitialGeneratedKey = (string)jObject["initial_generated_key"] ?? null,
            VerificationCode = (string)jObject["verification_code"] ?? null,
            User = jObject["user_uid"].ToObject<User>() ?? null,
            StartedAt = jObject["started_at"]?.ToObject<DateTime?>() ?? null,
        };

        return accountClaimAuth;
    }


    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _stoppingCts.Cancel();
        await Task.WhenAny(_listeningTask, Task.Delay(Timeout.Infinite, cancellationToken));
        _stoppingCts.Dispose();
    }
}
