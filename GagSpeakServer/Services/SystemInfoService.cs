using Gagspeak.API.Dto;
using Gagspeak.API.SignalR;
using GagspeakServer.Hubs;
using GagspeakServer.Data;
using GagspeakServer.Services;
using GagspeakServer.Utils.Configuration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GagspeakServer.Services;

public class SystemInfoService : IHostedService, IDisposable
{
    private readonly IConfigService<ServerConfiguration> _config;
    private readonly IServiceProvider _services;
    private readonly ILogger<SystemInfoService> _logger;
    private readonly IHubContext<GagspeakHub, IGagspeakHub> _hubContext;
    private Timer _timer;
    public SystemInfoDto SystemInfoDto { get; private set; } = new();

    public SystemInfoService(IConfigService<ServerConfiguration> configurationService, IServiceProvider services,
        ILogger<SystemInfoService> logger, IHubContext<GagspeakHub, IGagspeakHub> hubContext)
    {
        _config = configurationService;
        _services = services;
        _logger = logger;
        _hubContext = hubContext;
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

            var onlineUsers = 1337; //(_redis.SearchKeysAsync("UID:*").GetAwaiter().GetResult()).Count();
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