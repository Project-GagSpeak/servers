using GagspeakDiscord;
using GagspeakShared.Data;
using GagspeakShared.Services;
using GagspeakShared.Utils.Configuration;

// the main program class for the gagspeak discord bot / discord center.
public class Program
{
    // The entry point of the application.
    public static void Main(string[] args)
    {
        // Create the host builder.
        var hostBuilder = CreateHostBuilder(args);
        // Build the host.
        var host = hostBuilder.Build();

        // Create a scope for dependency injection.
        using (var scope = host.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            // Create an instance of the GagspeakDbContext.
            using var dbContext = services.GetRequiredService<GagspeakDbContext>();

            // Get the Discord configuration options.
            var options = host.Services.GetService<IConfigurationService<DiscordConfiguration>>();
            // Get the Server configuration options.
            var optionsServer = host.Services.GetService<IConfigurationService<ServerConfiguration>>();
            // Get the logger instance for the program class
            var logger = host.Services.GetService<ILogger<Program>>();

            if (optionsServer != null)
            {
                logger.LogInformation("Loaded Gagspeak Server Configuration (IsMain: {isMain})", optionsServer.IsMain);
                logger.LogInformation(optionsServer.ToString());
            }
            else
            {
                logger.LogWarning("ServerConfiguration options are null.");
            }

            if (options != null)
            {
                logger.LogInformation("Loaded Gagspeak Services Configuration (IsMain: {isMain})", options.IsMain);
                logger.LogInformation(options.ToString());
            }
            else
            {
                logger.LogWarning("DiscordConfiguration options are null.");
            }
        }

        // Run the host.
        host.Run();
    }

    /// <summary> Ran when the program executes to create the host builder.
    /// <para>
    /// This create host builder will use the startup from the startup class, 
    /// in turn executing it when the host runs later on
    /// </para>
    /// </summary>
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSystemd()
            .UseConsoleLifetime()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                // Use the base directory as the content root.
                webBuilder.UseContentRoot(AppContext.BaseDirectory);
                webBuilder.ConfigureLogging((ctx, builder) =>
                {
                    // Add the logging configuration.
                    builder.AddConfiguration(ctx.Configuration.GetSection("Logging"));
                    // Add the file logger.
                    builder.AddFile(o => o.RootPath = AppContext.BaseDirectory);
                });
                // Use the Startup class as the startup for the web host.
                webBuilder.UseStartup<Startup>();
            });
}
