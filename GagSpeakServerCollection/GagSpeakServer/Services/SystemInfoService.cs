using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakServer.Hubs;
using GagspeakShared.Data;
using GagspeakShared.Metrics;
using GagspeakShared.Services;
using GagspeakShared.Utils.Configuration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace GagspeakServer.Services;

public sealed class SystemInfoService : BackgroundService
{
    private readonly GagspeakMetrics _metrics;
    private readonly IConfigurationService<ServerConfiguration> _config;
    private readonly IDbContextFactory<GagspeakDbContext> _dbContextFactory;
    private readonly ILogger<SystemInfoService> _logger;
    private readonly IHubContext<GagspeakHub, IGagspeakHub> _hubContext;
    private readonly IRedisDatabase _redis;
    public ServerInfoResponse SystemInfoDto { get; private set; } = new(0);

    public SystemInfoService(GagspeakMetrics metrics, IConfigurationService<ServerConfiguration> config, 
        IDbContextFactory<GagspeakDbContext> dbContextFactory, ILogger<SystemInfoService> logger, 
        IHubContext<GagspeakHub, IGagspeakHub> hubContext, IRedisDatabase redis)
    {
        _metrics = metrics;
        _config = config;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _hubContext = hubContext;
        _redis = redis;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("System Info Service started");
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var timeOut = _config.IsMain ? 15 : 60;

        while (!ct.IsCancellationRequested)
        {
            try
            {
                ThreadPool.GetAvailableThreads(out int workerThreads, out int ioThreads);
                _metrics.SetGaugeTo(MetricsAPI.GaugeAvailableWorkerThreads, workerThreads);
                _metrics.SetGaugeTo(MetricsAPI.GaugeAvailableIOWorkerThreads, ioThreads);

                int onlineUsers = (_redis.SearchKeysAsync("GagspeakHub:UID:*").GetAwaiter().GetResult()).Count();
                SystemInfoDto = new ServerInfoResponse(onlineUsers);
                if (_config.IsMain)
                {
                    _logger.LogInformation($"Pushing system info: [{onlineUsers} users online]");
                    await _hubContext.Clients.All.Callback_ServerInfo(SystemInfoDto).ConfigureAwait(false);
                    using var db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

                    // lower how many things are being tracked by the db if it becomes too much.
                    _metrics.SetGaugeTo(MetricsAPI.GaugeAuthorizedConnections, onlineUsers);
                    _metrics.SetGaugeTo(MetricsAPI.GaugePairs, db.ClientPairs.AsNoTracking().Count());
                    _metrics.SetGaugeTo(MetricsAPI.GaugeUsersRegistered, db.Users.AsNoTracking().Count());
                }

                await Task.Delay(TimeSpan.FromSeconds(timeOut), ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to push system info");
            }
        }
    }
}