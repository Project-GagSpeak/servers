using Gagspeak.API.Dto;
using Gagspeak.API.SignalR;
using GagspeakServer.Hubs;
using GagspeakServer.Data;
using GagspeakServer.Services;
using GagspeakServer.Utils.Configuration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis.Extensions.Core.Abstractions;
using GagspeakServer.Metrics;

namespace GagspeakServer.Services;

public class SystemInfoService : IHostedService, IDisposable
{
    private readonly GagspeakMetrics _metrics;
    private readonly IConfigService<ServerConfiguration> _config;
    private readonly IServiceProvider _services;
    private readonly ILogger<SystemInfoService> _logger;
    private readonly IHubContext<GagspeakHub, IGagspeakHub> _hubContext;
    private readonly IRedisDatabase _redis;
    private Timer _timer;
    public SystemInfoDto SystemInfoDto { get; private set; } = new();

    public SystemInfoService(GagspeakMetrics metrics, IConfigService<ServerConfiguration> configurationService, IServiceProvider services,
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

        var timeOut = _config.IsMain ? 120 : 300;

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

            var onlineUsers = (_redis.SearchKeysAsync("UID:*").GetAwaiter().GetResult()).Count();
            SystemInfoDto = new SystemInfoDto()
            {
                OnlineUsers = onlineUsers,
            };

            if (_config.IsMain)
            {
                _logger.LogInformation("Sending System Info, Online Users: {onlineUsers}", onlineUsers);

                _hubContext.Clients.All.Client_UpdateSystemInfo(SystemInfoDto);

                using var scope = _services.CreateScope();
                using var db = scope.ServiceProvider.GetService<GagspeakDbContext>()!;

                _metrics.SetGaugeTo(MetricsAPI.GaugeAuthorizedConnections, onlineUsers);
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