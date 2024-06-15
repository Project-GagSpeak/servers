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
using MareSynchronosAuthService.Controllers;
using MareSynchronosShared.Metrics;
using MareSynchronosShared.Services;
using MareSynchronosShared.Utils;
using Microsoft.AspNetCore.Mvc.Controllers;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.System.Text.Json;
using StackExchange.Redis;
using System.Net;
using MareSynchronosAuthService.Services;
using MareSynchronosShared.RequirementHandlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MareSynchronosShared.Data;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using MareSynchronosShared.Utils.Configuration;
using SixLabors.ImageSharp;

namespace GagspeakServer;
public class Startup
{
    private readonly IConfiguration _configuration; // the configuration object
    private ILogger<Startup> _logger; // the logger object for the startup class.

    public Startup(IConfiguration configuration, ILogger<Startup> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary> The primary server configurator. It is responsible for configuring all the major components of the Server. </summary>
    /// <para> The method handles configuration for GAGSPEAK-METRICS, DATABASE, AUTHORIZATION, SIGNALR & REDIS, GAGSPEAK-SERVICES </para>
    /// </summary>
    /// <param name="services">The sevice collection for the Server</param>
    public void ConfigureServices(IServiceCollection services)
    {
        // begin by adding the http context accessor
        services.AddHttpContextAccessor();

        // add a transient service for the configuration
        services.AddTransient(_ => _configuration);

        // create a configuration section var for the gagspeak config
        IConfigurationSection gagspeakConfig = _configuration.GetRequiredSection("GagSpeak");

        // handle the configuration for our metrics
        ConfigureMetrics(services);

        // handle the startup configuration for our database
        ConfigureDatabase(services, mareConfig);

        // handle the startup configuration for our authorization module
        ConfigureAuthorization(services);

        // handle the startup configuration for our signalR module
        ConfigureSignalR(services, mareConfig);

        // append the client health checks
        services.AddHealthChecks();
        // ammend the apicontrollers application part manager
        services.AddControllers().ConfigureApplicationPartManager(a =>
        {
            a.FeatureProviders.Remove(a.FeatureProviders.OfType<ControllerFeatureProvider>().First());
            if (gagspeakConfig.GetValue<Uri>(nameof(ServerConfiguration.MainServerAddress), defaultValue: null) == null)
            {
                a.FeatureProviders.Add(new AllowedControllersFeatureProvider(typeof(MareServerConfigurationController), typeof(MareBaseConfigurationController), typeof(ClientMessageController)));
            }
            else
            {
                a.FeatureProviders.Add(new AllowedControllersFeatureProvider());
            }
        });





        /// <summary> The configurator for our application and enviorment. </summary>
        /// <param name="app">The application builder</param>
        /// <param name="env">The web host enviorment</param>
        /// <param name="logger">The logger for the startup class</param>
        public void ConfigureAppEnv(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
    {
        // define the config var from the App services, and call upon our GagspeakConfigBase
        var config = app.ApplicationServices.GetRequiredService<IConfigService<GagspeakConfigBase>>();

        // we want to use routing
        app.UseRouting();

        // we want to use HTTPMetrics for our metrics (created thanks to Prometheus)
        app.UseHttpMetrics();

        // we want to use authentication and authorization
        app.UseAuthentication();
        app.UseAuthorization();

        
        // A stand-alone Kestrel based metric server that saves you the effort of setting up the ASP.NET Core pipeline.
        // For all practical purposes, this is just a regular ASP.NET Core server that only serves Prometheus requests.
        KestrelMetricServer metricServer = new KestrelMetricServer(config.GetValueOrDefault<int>(nameof(GagspeakConfigBase.MetricsPort), 4985));


        services.AddHttpContextAccessor(); // requires to prevent server spitting invalid httpcontext accessor errors.

        // create a gagspeak config in our configuration under a section we define
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
