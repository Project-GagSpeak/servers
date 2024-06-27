using GagspeakShared.Data;
using GagspeakShared.Metrics;
using GagspeakShared.Services;
using GagspeakShared.Utils;
using GagspeakShared.Utils.Configuration;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using StackExchange.Redis;

namespace GagspeakDiscord;

/// <summary> The startup class for the discord component of the gagspeak server applications.
/// <para> NOTE: There is no controller in this component because it is a discord bot, and the discord bot is not a web server. </para
/// </summary>
public class Startup
{
    public Startup(IConfiguration config)
    {
        _config = config;
    }

    public IConfiguration _config { get; }

    // the app enviorment configure function, fore setting up the metrics server and webmap endpoints.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // first, get the config service
        var config = app.ApplicationServices.GetRequiredService<IConfigurationService<GagspeakConfigurationBase>>();

        // next, set up the metrics server at the port specified in the config located in the appsettings.json file. Otherwise, use port 4982.
        var metricServer = new KestrelMetricServer(
                config.GetValueOrDefault<int>(nameof(GagspeakConfigurationBase.MetricsPort), 4982));
        // start the metrics server
        metricServer.Start();


        // configure gagspeak's discord servers to use routing
        app.UseRouting();
        // and set the endpoints to a dummyhub. The dummyhub is a hack for redi's (thanks mare again for figuring this out).
        app.UseEndpoints(e =>
        {
            e.MapHub<GagspeakServer.Hubs.GagspeakHub>("/dummyhub");
        });
    }

    /// <summary> Configure the general services for the gagspeak discord server </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        // get the gagspeak config
        var gagSpeakConfigSection = _config.GetSection("GagSpeak");
        if (!gagSpeakConfigSection.Exists())
        {
            throw new InvalidOperationException("Section 'GagSpeak' not found in configuration.");
        }

        var redisConnectionString = gagSpeakConfigSection["RedisConnectionString"];
        if (string.IsNullOrEmpty(redisConnectionString))
        {
            throw new ArgumentException("RedisConnectionString in 'GagSpeak' section is empty.");
        }

        // add the gagspeak database context to the services. Be sure we set it up with the correct options.
        services.AddDbContextPool<GagspeakDbContext>(options =>
        {
            // use postgresql as the database provider, with the connection string from the appsettings.json file.
            options.UseNpgsql(_config.GetConnectionString("DefaultConnection"), builder =>
            {
                // build it with the ef migrationshisstory in the public schema, using snakecasenames.
                builder.MigrationsHistoryTable("_efmigrationshistory", "public");
            }).UseSnakeCaseNamingConvention();
            options.EnableThreadSafetyChecks(false); // do not include thread safety checks
        }, _config.GetValue(nameof(GagspeakConfigurationBase.DbContextPoolSize), 1024)); // set the pool size to the value in the configBase, or 1024

        // add the gagspeak metrics to the services, this is pulled from the shared service library.
        services.AddSingleton(m => new GagspeakMetrics(m.GetService<ILogger<GagspeakMetrics>>(), new List<string> { }, new List<string> { }));

        // set redi's to the connection string in the appsettings.json file.
        var redis = gagSpeakConfigSection.GetValue(nameof(ServerConfiguration.RedisConnectionString), string.Empty);
        // parse the options from the config options from the redi's string we got
        var options = ConfigurationOptions.Parse(redis);
        // set the client name to gagspeak, and the channel prefix to UserData
        options.ClientName = "GagSpeak";
        options.ChannelPrefix = "UserData";
        // configure the connection multiplexer for the redi's connection
        ConnectionMultiplexer connectionMultiplexer = ConnectionMultiplexer.Connect(options);
        // add it as a service so it gets registered
        services.AddSingleton<IConnectionMultiplexer>(connectionMultiplexer);

        // construct the signalR service builder with the options specified in the lambda
        var signalRServiceBuilder = services.AddSignalR(hubOptions =>
        {
            hubOptions.MaximumReceiveMessageSize = long.MaxValue;
            hubOptions.EnableDetailedErrors = true;
            hubOptions.MaximumParallelInvocationsPerClient = 10;
            hubOptions.StreamBufferCapacity = 200;
        })
        .AddMessagePackProtocol(opt => // then add the messagepack protocol with the resolver options (i just pulled them, i dont get what they mean lol)
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

            // and set the options serializer options to standard with lz4block compression and the resolver we just made
            opt.SerializerOptions = MessagePackSerializerOptions.Standard
                .WithCompression(MessagePackCompression.Lz4Block)
                .WithResolver(resolver);
        });

        // now we can pull the string of our redi's connectionstring to get the connection string.
        var redisConnection = gagSpeakConfigSection.GetValue(nameof(GagspeakConfigurationBase.RedisConnectionString), string.Empty);
        // and add the redi's connection to the signalR service builder with the options we made above.
        // (because we use signalR, this is why we have to use the hack for the dummyhub)
        signalRServiceBuilder.AddStackExchangeRedis(redisConnection, options => { });

        // now we can add the gagspeak discord configure, singleton, hosted service, and interfaces to the service collection.
        services.Configure<DiscordConfiguration>(_config.GetRequiredSection("GagSpeak"));
        services.Configure<ServerConfiguration>(_config.GetRequiredSection("GagSpeak")); // everything should be preset already
        services.Configure<GagspeakConfigurationBase>(_config.GetRequiredSection("GagSpeak"));

        services.AddSingleton(_config);
        services.AddSingleton<ServerTokenGenerator>();
        services.AddSingleton<DiscordBotServices>();

        services.AddHostedService<DiscordBot>(); // add the discord bot as a hosted service

        services.AddSingleton<IConfigurationService<DiscordConfiguration>, GagspeakConfigServiceServer<DiscordConfiguration>>();
        services.AddSingleton<IConfigurationService<ServerConfiguration>, GagspeakConfigServiceClient<ServerConfiguration>>();
        services.AddSingleton<IConfigurationService<GagspeakConfigurationBase>, GagspeakConfigServiceClient<GagspeakConfigurationBase>>();

        services.AddHostedService(p => (GagspeakConfigServiceClient<GagspeakConfigurationBase>)p.GetService<IConfigurationService<GagspeakConfigurationBase>>());
        services.AddHostedService(p => (GagspeakConfigServiceClient<ServerConfiguration>)p.GetService<IConfigurationService<ServerConfiguration>>());
    }
}