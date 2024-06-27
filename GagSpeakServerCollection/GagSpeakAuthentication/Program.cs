namespace GagspeakAuthentication;

// the main program class for the gagspeak authentication server center.
public class Program
{
    public static void Main(string[] args)
    {
        // Create the host builder. (calls function below)
        var hostBuilder = CreateHostBuilder(args);
        // after host is created, build it and assign it to an IHost object.
        using var host = hostBuilder.Build();
        // try and run the host
        try
        {
            host.Run();
        }
        // but if it fails, throw exception
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    /// <summary> Method to create the host builder for the gagspeak authentication server. </summary>
    /// <param name="args"> The arguments passed to the program. </param>
    /// <returns> The host builder for the gagspeak authentication server. </returns>
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        // create a logger factory for the program for the builder object that can create new loggers.
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders();
            builder.AddConsole();
        });
        // use it to make a new logger for the Startup class in the gagspeak authentication server.
        var logger = loggerFactory.CreateLogger<Startup>();
        // return a new default builder with some arguements -->
        return Host.CreateDefaultBuilder(args)
            // use the systemd for the host
            .UseSystemd()
            // use the console lifetime for the host
            .UseConsoleLifetime()
            // configure the web host defaults to...
            .ConfigureWebHostDefaults(webBuilder =>
            {
                // use the content root as the base directory
                webBuilder.UseContentRoot(AppContext.BaseDirectory);
                // configure logging for the auth service
                webBuilder.ConfigureLogging((ctx, builder) =>
                {
                    builder.AddConfiguration(ctx.Configuration.GetSection("Logging"));
                    builder.AddFile(o => o.RootPath = AppContext.BaseDirectory);
                });
                // and to use the startup as the startup class for the web host
                webBuilder.UseStartup(ctx => new Startup(ctx.Configuration, logger));
            });
    }
}
