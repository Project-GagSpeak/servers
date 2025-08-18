
using GagspeakShared.Data;
using GagspeakShared.Metrics;
using GagspeakShared.Services;
using GagspeakShared.Utils.Configuration;
using Microsoft.EntityFrameworkCore;
using Prometheus;

namespace GagspeakServer;

/// <summary> The primary program class for the GagSpeakServer. Calls Main() </summary>
public class Program
{
    public static void Main(string[] args)
    {
        // define the host builder variable, and call createHostBuilder (see below)
        IHostBuilder hostBuilder = CreateHostBuilder(args);
        // once made, construct the IHost object from the host builder
        using IHost host = hostBuilder.Build();
        // then, create a scope from the host services
        using (IServiceScope scope = host.Services.CreateScope())
        {
            // in the scope, define the services as the service provider
            IServiceProvider services = scope.ServiceProvider;
            // then, get the gagspeak database context from the services
            using GagspeakDbContext context = services.GetRequiredService<GagspeakDbContext>();
            // next, lets get the options from the services under the server config IConfigurationService
            IConfigurationService<ServerConfiguration> options = services.GetRequiredService<IConfigurationService<ServerConfiguration>>();
            // now, snag the logger for the program class
            ILogger<Program> logger = host.Services.GetRequiredService<ILogger<Program>>();

            // in the options, if it is the main server,
            if (options.IsMain)
            {
                // set the command timeout to 10 minutes
                context.Database.SetCommandTimeout(TimeSpan.FromMinutes(10));
                // and migrate the database
                context.Database.Migrate();
                // update the timeout after migration
                context.Database.SetCommandTimeout(TimeSpan.FromSeconds(30));
                // and save the changes to the database
                context.SaveChanges();

                // finally, we will need to cleanup the unfinished registrations 
                IQueryable<GagspeakShared.Models.AccountClaimAuth> unfinishedRegistrations = context.AccountClaimAuth.Where(c => c.StartedAt != null);
                context.RemoveRange(unfinishedRegistrations);
                context.SaveChanges();

                logger.LogInformation(options.ToString());
            }

            // finally, we will get the mare metrics and setup the gauges 
            GagspeakMetrics metrics = services.GetRequiredService<GagspeakMetrics>();

            metrics.SetGaugeTo(MetricsAPI.GaugeUsersRegistered, context.Users.AsNoTracking().Count());
            metrics.SetGaugeTo(MetricsAPI.GaugePairs, context.ClientPairs.AsNoTracking().Count());
            metrics.SetGaugeTo(MetricsAPI.GaugePatternLikes, context.LikesPatterns.AsNoTracking().Count());
            metrics.SetGaugeTo(MetricsAPI.GaugeMoodleLikes, context.LikesMoodles.AsNoTracking().Count());
            metrics.SetGaugeTo(MetricsAPI.GaugeShareHubPatterns, context.Patterns.AsNoTracking().Count());
            metrics.SetGaugeTo(MetricsAPI.GaugeShareHubMoodles, context.Moodles.AsNoTracking().Count());
            metrics.SetGaugeTo(MetricsAPI.GaugePendingKinksterRequests, context.KinksterPairRequests.AsNoTracking().Count());
            metrics.SetGaugeTo(MetricsAPI.GaugePendingCollarRequests, context.CollarRequests.AsNoTracking().Count());
        }

        // now that the scope is done, we will check if the first argument is "dry"
        if (args.Length == 0 || !string.Equals(args[0], "dry", StringComparison.Ordinal))
        {
            // if it is not, run the host
            try
            {
                host.Run();
            }
            catch (Exception ex)
            {
                // if there is an exception, log it
                Console.WriteLine(ex);
            }
        }
    }

    /// <summary> Called by the IHostBuilder to create the host builder. </summary>
    /// <returns> The IHostBuilder object. </returns>
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        // create a new logger factory so we can formulate a logger
        // for the startup class to make sure everything is working
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddConsole();
        });
        // use the logger factory to create the logger for the startup class here
        var logger = loggerFactory.CreateLogger<Startup>();
        // then create the default builder for the host
        return Host.CreateDefaultBuilder(args)
            .UseSystemd()
            .UseConsoleLifetime()
            .ConfigureAppConfiguration((ctx, config) =>
            {
                var appSettingsPath = Environment.GetEnvironmentVariable("APPSETTINGS_PATH");
                if (!string.IsNullOrEmpty(appSettingsPath))
                    config.AddJsonFile(appSettingsPath, optional: true, reloadOnChange: true);
                else
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                // add other environmental variables defined within the service.
                config.AddEnvironmentVariables();
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseContentRoot(AppContext.BaseDirectory);
                webBuilder.ConfigureLogging((ctx, builder) =>
                {
                    builder.AddConfiguration(ctx.Configuration.GetSection("Logging"));
                    builder.AddFile(o => o.RootPath = AppContext.BaseDirectory);
                });
                // finally, use the startup class we defined below, (head over to the startup.cs)
                webBuilder.UseStartup(ctx => new Startup(ctx.Configuration, logger));
            });
    }
}