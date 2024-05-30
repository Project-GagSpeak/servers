using Microsoft.EntityFrameworkCore;
using GagSpeakShared.Data;
using GagSpeakShared.Metrics;
using GagSpeakShared.Services;
using GagSpeakShared.Utils.Configuration;

namespace GagSpeakServer;

public class Program
{
    public static void Main(string[] args)
    {
        var hostBuilder = CreateHostBuilder(args);
        using var host = hostBuilder.Build();
        using (var scope = host.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            using var context = services.GetRequiredService<GagSpeakDbContext>();
            var options = services.GetRequiredService<IConfigurationService<ServerConfiguration>>();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();

            if (options.IsMain)
            {
                context.Database.SetCommandTimeout(TimeSpan.FromMinutes(10));
                context.Database.Migrate();
                context.Database.SetCommandTimeout(TimeSpan.FromSeconds(30));
                context.SaveChanges();

                // clean up residuals
                var looseFiles = context.Files.Where(f => f.Uploaded == false);
                var unfinishedRegistrations = context.LodeStoneAuth.Where(c => c.StartedAt != null);
                context.RemoveRange(unfinishedRegistrations);
                context.RemoveRange(looseFiles);
                context.SaveChanges();

                logger.LogInformation(options.ToString());
            }

            var metrics = services.GetRequiredService<GagSpeakMetrics>();

            metrics.SetGaugeTo(MetricsAPI.GaugeUsersRegistered, context.Users.AsNoTracking().Count());
            metrics.SetGaugeTo(MetricsAPI.GaugePairs, context.ClientPairs.AsNoTracking().Count());
            metrics.SetGaugeTo(MetricsAPI.GaugePairsPaused, context.Permissions.AsNoTracking().Count(p => p.IsPaused));

        }

        if (args.Length == 0 || !string.Equals(args[0], "dry", StringComparison.Ordinal))
        {
            try
            {
                host.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddConsole();
        });
        var logger = loggerFactory.CreateLogger<Startup>();
        return Host.CreateDefaultBuilder(args)
            .UseSystemd()
            .UseConsoleLifetime()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseContentRoot(AppContext.BaseDirectory);
                webBuilder.ConfigureLogging((ctx, builder) =>
                {
                    builder.AddConfiguration(ctx.Configuration.GetSection("Logging"));
                    builder.AddFile(o => o.RootPath = AppContext.BaseDirectory);
                });
                webBuilder.UseStartup(ctx => new Startup(ctx.Configuration, logger));
            });
    }
}
