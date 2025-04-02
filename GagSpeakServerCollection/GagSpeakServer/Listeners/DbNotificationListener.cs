using GagspeakAPI.SignalR;
using GagspeakServer.Hubs;
using GagspeakShared.Data;
using GagspeakShared.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
#nullable disable

namespace GagspeakServer.Listeners;
public class DbNotificationListener : IHostedService
{
    private readonly IDbContextFactory<GagspeakDbContext> _dbContextFactory;
    private readonly IHubContext<GagspeakHub, IGagspeakHub> _hubContext;
    private readonly ILogger<DbNotificationListener> _logger;
    private readonly string _connectionString = string.Empty;
    private Task _listeningTask = Task.CompletedTask;
    private CancellationTokenSource _stoppingCts = new CancellationTokenSource();

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
        using NpgsqlConnection connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(_stoppingCts.Token).ConfigureAwait(false);
        using (NpgsqlCommand cmd = new NpgsqlCommand("LISTEN accountclaimauth_insert", connection))
        {
            cmd.ExecuteNonQuery();
        }

        connection.Notification += async (o, e) =>
        {
            _logger.LogInformation($"[[ Notification received]] : {e.Payload}");
            try
            {
                AccountClaimAuth accountClaimAuth = ParsePayloadToAccountClaimAuth(e.Payload);
                List<AccountClaimAuth> accountClaims = new List<AccountClaimAuth> { accountClaimAuth };
                // dont allow any entries in with empty initial keys or if the size is 0
                if (string.IsNullOrEmpty(accountClaimAuth.InitialGeneratedKey) || accountClaimAuth.InitialGeneratedKey.Length == 0)
                {
                    _logger.LogError($"[[ Notification error]] : InitialGeneratedKey is empty or null");
                    return;
                }
                _logger.LogInformation($"[[ Notification parsed]] : {accountClaims.Count} account claims received");

                // execute the logic
                using GagspeakDbContext dbContext = _dbContextFactory.CreateDbContext();

                // for each authentication that was newly added
                foreach (AccountClaimAuth auth in accountClaims)
                {
                    // locate the auth in the database with the matching hashed key
                    Auth matchingUserAuth = await dbContext.Auth.AsNoTracking().SingleOrDefaultAsync(u => u.HashedKey == auth.InitialGeneratedKey).ConfigureAwait(false);
                    // if the auth object is null, then the auth object was not found in the database
                    if (matchingUserAuth is null)
                        continue;

                    // then locate the userUID of that auth object
                    string userUID = matchingUserAuth.UserUID;

                    // see if that user UID is in the list of user connections
                    if (!string.IsNullOrEmpty(userUID))
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
            await connection.WaitAsync(_stoppingCts.Token).ConfigureAwait(false);
        }
    }

    public AccountClaimAuth ParsePayloadToAccountClaimAuth(string jsonPayload)
    {
        var jObject = JObject.Parse(jsonPayload);

        AccountClaimAuth accountClaimAuth = new AccountClaimAuth
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
        await Task.WhenAny(_listeningTask, Task.Delay(Timeout.Infinite, cancellationToken)).ConfigureAwait(false);
        _stoppingCts.Dispose();
    }
}
#nullable restore
