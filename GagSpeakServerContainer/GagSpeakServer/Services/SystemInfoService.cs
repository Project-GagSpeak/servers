using GagSpeak.API.Dto;
using GagSpeak.API.SignalR;
using GagSpeakServer.Hubs;
using GagSpeakShared.Data;
using GagSpeakShared.Metrics;
using GagSpeakShared.Services;
using GagSpeakShared.Utils.Configuration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis.Extensions.Core.Abstractions;

namespace GagSpeakServer.Services;

public class SystemInfoService : IHostedService, IDisposable
{
    private readonly GagSpeakMetrics _GagSpeakMetrics;
    private readonly IConfigurationService<ServerConfiguration> _config;
    private readonly IServiceProvider _services;
    private readonly ILogger<SystemInfoService> _logger;
    private readonly IHubContext<GagSpeakHub, IGagSpeakHub> _hubContext;
    private readonly IRedisDatabase _redis;
    private Timer _timer;
    public SystemInfoDto SystemInfoDto { get; private set; } = new();

    public SystemInfoService(GagSpeakMetrics GagSpeakMetrics, IConfigurationService<ServerConfiguration> configurationService, IServiceProvider services,
        ILogger<SystemInfoService> logger, IHubContext<GagSpeakHub, IGagSpeakHub> hubContext, IRedisDatabase redisDb)
    {
        _GagSpeakMetrics = GagSpeakMetrics;
        _config = configurationService;
        _services = services;
        _logger = logger;
        _hubContext = hubContext;
        _redis = redisDb;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("System Info Service started");

        var timeOut = _config.IsMain ? 5 : 15;

        _timer = new Timer(PushSystemInfo, null, TimeSpan.Zero, TimeSpan.FromSeconds(timeOut));

        return Task.CompletedTask;
    }

    private void PushSystemInfo(object state)
    {
        try
        {
            ThreadPool.GetAvailableThreads(out int workerThreads, out int ioThreads);

            _GagSpeakMetrics.SetGaugeTo(MetricsAPI.GaugeAvailableWorkerThreads, workerThreads);
            _GagSpeakMetrics.SetGaugeTo(MetricsAPI.GaugeAvailableIOWorkerThreads, ioThreads);

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
                using var db = scope.ServiceProvider.GetService<GagSpeakDbContext>()!;

                _GagSpeakMetrics.SetGaugeTo(MetricsAPI.GaugeAuthorizedConnections, onlineUsers);
                _GagSpeakMetrics.SetGaugeTo(MetricsAPI.GaugePairs, db.ClientPairs.AsNoTracking().Count());
                _GagSpeakMetrics.SetGaugeTo(MetricsAPI.GaugePairsPaused, db.Permissions.AsNoTracking().Count(p => p.IsPaused));
                _GagSpeakMetrics.SetGaugeTo(MetricsAPI.GaugeGroups, db.Groups.AsNoTracking().Count());
                _GagSpeakMetrics.SetGaugeTo(MetricsAPI.GaugeGroupPairs, db.GroupPairs.AsNoTracking().Count());
                _GagSpeakMetrics.SetGaugeTo(MetricsAPI.GaugeUsersRegistered, db.Users.AsNoTracking().Count());
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