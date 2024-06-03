
using GagspeakServer.Data;
using GagspeakServer.Services;
using GagspeakServer.Utils.Configuration;

namespace GagspeakServer;
public class Program
{
    // the main entry point of the application
    public static void Main(string[] args)
    {
        // create the host builder variable
        var hostBuilder = CreateHostBuilder(args);
        // build the host
        using var host = hostBuilder.Build();

        // configure the rest of the stuff in here.
        using var scope = host.Services.CreateScope();
        {
            // get the service provider
            var services = scope.ServiceProvider;
            // get the context for the database entries
            var context = services.GetRequiredService<GagspeakDbContext>();
            // get the options for the server configuration service
            var options = services.GetRequiredService<IConfigService<ServerConfiguration>>();
            // get the logger
            var logger = services.GetRequiredService<ILogger<Program>>();
            // log the start of the application
            logger.LogInformation("Starting application");
        }

        // we need to make sure everything is ready to run
        if (args.Length == 0 || !string.Equals(args[0], "dry", StringComparison.Ordinal))
        {
            // try to run the host
            try
            {
                // run the host
                host.Run();
            }
            // catch any exceptions
            catch (Exception ex)
            {
                // log the exception
                Console.WriteLine(ex);
            }
        }        
    }

    // how we create the host builder
    public static IHostBuilder CreateHostBuilder(string[] args) 
    {
        // first establish a logger factory so that we can output the actions]
        // of the application to the console
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.ClearProviders(); // clear the default providers
            builder.AddConsole(); // add the console provider
        });
        // create a logger for the startup
        var logger = loggerFactory.CreateLogger<Startup>();
        // return the host builder
        return Host.CreateDefaultBuilder(args)
            // configure the web builder host defaults
            .ConfigureWebHostDefaults(webBuilder =>
            {
                // use the startup class
                webBuilder.UseStartup<Startup>();
            });
    }
}
