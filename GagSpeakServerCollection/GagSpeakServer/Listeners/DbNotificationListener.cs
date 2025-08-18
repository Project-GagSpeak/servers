using GagspeakAPI.Hub;
using GagspeakServer.Hubs;
using GagspeakShared.Data;
using GagspeakShared.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text.Json;
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
                AccountClaimAuth auth = ParsePayloadToAccountClaimAuth(e.Payload);

                if (string.IsNullOrEmpty(auth.InitialGeneratedKey))
                {
                    _logger.LogError("[[ Notification error]] : InitialGeneratedKey is empty or null");
                    return;
                }

                // execute the logic
                using GagspeakDbContext dbContext = _dbContextFactory.CreateDbContext();

                var matchingUserAuth = await dbContext.Auth.AsNoTracking()
                    .SingleOrDefaultAsync(u => u.HashedKey == auth.InitialGeneratedKey)
                    .ConfigureAwait(false);

                if (matchingUserAuth is not null && !string.IsNullOrEmpty(matchingUserAuth.UserUID))
                    await _hubContext.Clients.User(matchingUserAuth.UserUID)
                        .Callback_ShowVerification(new() { Code = auth.VerificationCode ?? "" })
                        .ConfigureAwait(false);
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

    /// <summary>
    ///     The AccountClaimAuth that was inserted into the database by intercepting the JSON payload and turning it into the model version.
    /// </summary>
    public AccountClaimAuth ParsePayloadToAccountClaimAuth(string jsonPayload)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(jsonPayload);
            JsonElement root = doc.RootElement;

            return new AccountClaimAuth
            {
                DiscordId = root.GetProperty("discord_id").GetUInt64(),
                InitialGeneratedKey = root.GetProperty("initial_generated_key").GetString(),
                VerificationCode = root.TryGetProperty("verification_code", out JsonElement codeProp) ? codeProp.GetString() : null,
                User = root.TryGetProperty("user_uid", out JsonElement userProp)
                    ? JsonSerializer.Deserialize<User>(userProp.GetRawText())
                    : null,
                StartedAt = root.TryGetProperty("started_at", out JsonElement dateProp)
                    ? dateProp.Deserialize<DateTime?>()
                    : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[[Json Parsing Error]] Failed to parse payload: {Payload}", jsonPayload);
            throw;
        }
    }


    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _stoppingCts.Cancel();
        await Task.WhenAny(_listeningTask, Task.Delay(Timeout.Infinite, cancellationToken)).ConfigureAwait(false);
        _stoppingCts.Dispose();
    }
}
#nullable restore
