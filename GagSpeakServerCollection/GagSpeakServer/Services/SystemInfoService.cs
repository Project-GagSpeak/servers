using GagspeakAPI.SignalR;
using GagspeakAPI.Dto.Connection;
using GagspeakServer.Hubs;
using GagspeakShared.Data;
using GagspeakShared.Metrics;
using GagspeakShared.Services;
using GagspeakShared.Utils.Configuration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace GagspeakServer.Services;

public class SystemInfoService : IHostedService, IDisposable
{
    private readonly GagspeakMetrics _metrics;
    private readonly IConfigurationService<ServerConfiguration> _config;
    private readonly IServiceProvider _services;
    private readonly ILogger<SystemInfoService> _logger;
    private readonly IHubContext<GagspeakHub, IGagspeakHub> _hubContext;
    private readonly IRedisDatabase _redis;
    private Timer _timer;
    public SystemInfoDto SystemInfoDto { get; private set; } = new();

    public SystemInfoService(GagspeakMetrics metrics, IConfigurationService<ServerConfiguration> configurationService, IServiceProvider services,
        ILogger<SystemInfoService> logger, IHubContext<GagspeakHub, IGagspeakHub> hubContext, IRedisDatabase redis)
    {
        _metrics = metrics;
        _config = configurationService;
        _services = services;
        _logger = logger;
        _hubContext = hubContext;
        _redis = redis;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("System Info Service started");

        var timeOut = _config.IsMain ? 15 : 60;

        _timer = new Timer(PushSystemInfo, null, TimeSpan.Zero, TimeSpan.FromSeconds(timeOut));

        return Task.CompletedTask;
    }

    private void PushSystemInfo(object state)
    {
        try
        {
            ThreadPool.GetAvailableThreads(out int workerThreads, out int ioThreads);

            _metrics.SetGaugeTo(MetricsAPI.GaugeAvailableWorkerThreads, workerThreads);
            _metrics.SetGaugeTo(MetricsAPI.GaugeAvailableIOWorkerThreads, ioThreads);

            var gagspeakOnlineUsers = (_redis.SearchKeysAsync("GagspeakHub:UID:*").GetAwaiter().GetResult()).Count();
            var toyboxOnlineUsers = (_redis.SearchKeysAsync("ToyboxHub:UID:*").GetAwaiter().GetResult()).Count();

            SystemInfoDto = new SystemInfoDto()
            {
                OnlineUsers = gagspeakOnlineUsers, // Specific to GagspeakHub
                OnlineToyboxUsers = toyboxOnlineUsers, // Specific to ToyboxHub
            };

            if (_config.IsMain)
            {
                // can always just refer to discord bot for this number instead of letting it spam my logs.
                // _logger.LogInformation("Online Users: {onlineUsers}", gagspeakOnlineUsers);

                _hubContext.Clients.All.Client_UpdateSystemInfo(SystemInfoDto);

                using var scope = _services.CreateScope();
                using var db = scope.ServiceProvider.GetService<GagspeakDbContext>()!;

                _metrics.SetGaugeTo(MetricsAPI.GaugeAuthorizedConnections, gagspeakOnlineUsers);
                _metrics.SetGaugeTo(MetricsAPI.GaugePairs, db.ClientPairs.AsNoTracking().Count());
                _metrics.SetGaugeTo(MetricsAPI.GaugeUsersRegistered, db.Users.AsNoTracking().Count());
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to push system info");
        }
    }


    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}