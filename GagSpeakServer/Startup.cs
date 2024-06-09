using GagspeakServer.Data;
using Gagspeak.API.SignalR;
using GagspeakServer.Hubs;
using Microsoft.AspNetCore.Http.Connections;
using GagspeakServer.Utils.Configuration;
using GagspeakServer.Services;
using MessagePack.Resolvers;
using MessagePack;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using Microsoft.EntityFrameworkCore;
using GagspeakServer.Discord;
using GagspeakServer.Discord.Configuration;
using StackExchange.Redis;

namespace GagspeakServer;
public class Startup
{
    // create the logger object from our service provider
    //private readonly ILogger<Startup> _logger;

    // establish the constructor to initialize the config and logger to this instance of the startup

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
        //_logger = logger;
    }

    // define the configuration
    public IConfiguration Configuration { get; }


    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpContextAccessor(); // requires to prevent server spitting invalid httpcontext accessor errors.

        // create a mare config in our configuration under a section we define
        var gagspeakConfig = Configuration.GetSection("Gagspeak");

        // we need to fill our service collection container with the services we need, so we let's add them

        ConfigureDatabase(services, gagspeakConfig);

        ConfigureSignalR(services, gagspeakConfig);

        // see if we are running on the main server
        bool isMainServer = gagspeakConfig.GetValue<Uri>(nameof(ServerConfiguration.MainServerAddress), defaultValue: null) == null;

        // setting up a redi's connection for a multiplexer connection (still not sure WHY this is needed, but i keep getting recommended it for concurrency reasons)
        var redis = gagspeakConfig.GetValue(nameof(ServerConfiguration.RedisConnectionString), string.Empty);
        var options = ConfigurationOptions.Parse(redis);
        options.ClientName = "GagSpeak";
        options.ChannelPrefix = "UserData";
        ConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(options);
        services.AddSingleton<IConnectionMultiplexer>(connectionMultiplexer);

        // append the services we need
        services.Configure<DiscordConfiguration>(Configuration.GetRequiredSection("GagSpeak"));
        services.Configure<ServerConfiguration>(Configuration.GetRequiredSection("GagSpeak"));
        services.Configure<GagspeakConfigBase>(Configuration.GetRequiredSection("GagSpeak"));
        services.AddSingleton<IConfigService<DiscordConfiguration>, GagspeakConfigServiceServer<DiscordConfiguration>>();
        services.AddSingleton<IConfigService<ServerConfiguration>, GagspeakConfigServiceServer<ServerConfiguration>>();
        services.AddSingleton<IConfigService<GagspeakConfigBase>, GagspeakConfigServiceServer<GagspeakConfigBase>>();
        // add the singletons
        services.AddSingleton<SystemInfoService>();
        services.AddSingleton<SelectionDataService>();
        services.AddSingleton<SelectionBoardService>();

        // add singleton for the discord bot service
        // services.AddSingleton<ServerTokenGenerator>(); <--- Possibly needed later???
        services.AddSingleton<DiscordBotServices>();
        services.AddHostedService<DiscordBot>();
        
        // add the hosted provider
        services.AddHostedService(provider => provider.GetRequiredService<SystemInfoService>());

    }

    private static void ConfigureSignalR(IServiceCollection services, IConfigurationSection gagspeakConfig)
    {
        // let's add the service to our service provider,
        // services.AddSingleton<IUserIdProvider, IdBasedUserIdProvider>();

        // lets begin making the signalRservice builder
        var signalRServiceBuilder = services.AddSignalR(hubOptions =>
        {
            // we need to give signalR some options for our spesific hub
            hubOptions.MaximumReceiveMessageSize = long.MaxValue;
            hubOptions.EnableDetailedErrors = true;
            hubOptions.MaximumParallelInvocationsPerClient = 10;
            hubOptions.StreamBufferCapacity = 200;

            // add the filter limit later
        }).AddMessagePackProtocol(opt =>
        {
            var resolver = CompositeResolver.Create(StandardResolverAllowPrivate.Instance,
                BuiltinResolver.Instance,
                AttributeFormatterResolver.Instance,
                // replace enum resolver
                DynamicEnumAsStringResolver.Instance,
                DynamicGenericResolver.Instance,
                DynamicUnionResolver.Instance,
                DynamicObjectResolver.Instance,
                PrimitiveObjectResolver.Instance,
                // final fallback(last priority)
                StandardResolver.Instance);

            opt.SerializerOptions = MessagePackSerializerOptions.Standard
                .WithCompression(MessagePackCompression.Lz4Block)
                .WithResolver(resolver);
        });
    }

    private void ConfigureDatabase(IServiceCollection services, IConfigurationSection gagspeakConfig)
    {
        services.AddDbContextPool<GagspeakDbContext>(options =>
        {
            options.UseMySql(Configuration.GetConnectionString("DefaultConnection"),
                new MySqlServerVersion(new Version(8, 0, 29)),
            builder =>
            {
                builder.MigrationsHistoryTable("_efmigrationshistory", "public");
                builder.MigrationsAssembly("GagspeakShared"); // this doesnt exist but we can fix it later
            }).UseSnakeCaseNamingConvention();
            options.EnableThreadSafetyChecks(false);
        }, gagspeakConfig.GetValue(nameof(GagspeakConfigBase.DbContextPoolSize), 1024));
        services.AddDbContextFactory<GagspeakDbContext>(options =>
        {
            options.UseMySql(Configuration.GetConnectionString("DefaultConnection"),
                new MySqlServerVersion(new Version(8, 0, 29)),
            builder =>
            {
                builder.MigrationsHistoryTable("_efmigrationshistory", "public");
                builder.MigrationsAssembly("GagspeakShared");
            }).UseSnakeCaseNamingConvention();
            options.EnableThreadSafetyChecks(false);
        });
        //options =>
            // add options to use the defined sqlserver
            //options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
    {
        // log the start of the https request pipeline to the console
        logger.LogInformation("Starting the HTTP request pipeline -- Running Configure");

        // creater config varaible
        
        // if the enviorment is in development, we need to use the developer exception page
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        // use routing
        app.UseRouting();

        // allow it to use websockets
        app.UseWebSockets();

        // we should use authentication here at some point

        // establish our endpoints and mappings
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<GagspeakHub>(IGagspeakHub.Path, options =>
            {
                options.ApplicationMaxBufferSize = 5242880;
                options.TransportMaxBufferSize = 5242880;
                options.Transports = HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling;
            });
            // see the heartbeat of the server
            endpoints.MapHub<Connection>("/heartbeat");
            // see the user
            // endpoints.MapHub<User>("/user");
        });
    }
}
