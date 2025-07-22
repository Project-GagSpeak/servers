using GagspeakAPI.Hub;
using GagspeakServer.Hubs;
using GagspeakServer.Listeners;
using GagspeakServer.Services;
using GagspeakShared.Data;
using GagspeakShared.Metrics;
using GagspeakShared.RequirementHandlers;
using GagspeakShared.Services;
using GagspeakShared.Utils;
using GagspeakShared.Utils.Configuration;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.System.Text.Json;
using System.Net;
using System.Text;

namespace GagspeakServer;
public class Startup
{
    private readonly IConfiguration Configuration; // the configuration object
    private ILogger<Startup> _logger; // the logger object for the startup class.

    public Startup(IConfiguration configuration, ILogger<Startup> logger)
    {
        Configuration = configuration;
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
        services.AddTransient(_ => Configuration);

        // create a configuration section var for the gagspeak config
        IConfigurationSection gagspeakConfig = Configuration.GetRequiredSection("GagSpeak");

        // handle the startup configuration for our database
        ConfigureDatabase(services, gagspeakConfig);
        _logger.LogInformation("Database Configured");

        // handle the startup configuration for our authorization module
        ConfigureAuthorization(services);
        _logger.LogInformation("Authorization Configured");

        // handle the startup configuration for our signalR module
        ConfigureSignalR(services, gagspeakConfig, _logger);
        _logger.LogInformation("SignalR Configured");
        _logger.LogInformation("Redis Configured");

        // finally configure the GagspeakServices
        ConfigureGagspeakServices(services, gagspeakConfig);
        _logger.LogInformation("Gagspeak Services Configured");

        // append the client health checks
        services.AddHealthChecks();
        _logger.LogInformation("Health Checks Configured");

        // Thank you for the helpppppppp I really apperciate it <3
        services.AddControllers();
        _logger.LogInformation("Controllers Configured");
    }

    /// <summary> Helper method for configuring the Gagspeak Services </summary>
    private void ConfigureGagspeakServices(IServiceCollection services, IConfigurationSection gagspeakConfig)
    {
        // configure the server configurations for both the server config and the gagspeak config base.
        services.Configure<ServerConfiguration>(Configuration.GetRequiredSection("GagSpeak"));
        services.Configure<GagspeakConfigurationBase>(Configuration.GetRequiredSection("GagSpeak"));
        _logger.LogInformation("Server Configurations configured");
        // next, add the server token generator, systeminfo service, and online synced pair cache service to the services
        services.AddSingleton<ServerTokenGenerator>();
        services.AddSingleton<SystemInfoService>();
        _logger.LogInformation("Server Token Generator, System Info Service, and Online Synced Pair Cache Service added");

        // next, add the hosted service for the system info service
        services.AddHostedService(provider => provider.GetService<SystemInfoService>() ?? throw new InvalidOperationException("SystemInfoService not found"));
        _logger.LogInformation("System Info Service Hosted Service added");

        // configure services for the main server status
        services.AddSingleton<IConfigurationService<ServerConfiguration>, GagspeakConfigServiceServer<ServerConfiguration>>();
        services.AddSingleton<IConfigurationService<GagspeakConfigurationBase>, GagspeakConfigServiceServer<GagspeakConfigurationBase>>();
        _logger.LogInformation("Main Server Status Configurations configured");

        // add the services for the user cleanup so database isnt overloaded as fuck
        services.AddSingleton<UserCleanupService>();
        services.AddHostedService(provider => provider.GetService<UserCleanupService>() ?? throw new InvalidOperationException("UserCleanupService not found"));
        _logger.LogInformation("Cleanup services appended, and hosted service appended");

        // add the hosted service for the DBListener
        services.AddSingleton<DbNotificationListener>();
        services.AddHostedService(provider => provider.GetService<DbNotificationListener>() ?? throw new InvalidOperationException("DbNotificationListener not found"));
        _logger.LogInformation("NotificationListener added.");

    }

    /// <summary> Helper method for configuring the signalR service connectivity. </summary>
    private static void ConfigureSignalR(IServiceCollection services, IConfigurationSection gagspeakConfig, ILogger logger)
    {
        // first off, append the singleton for IdBasedUIDProviders, which fetches UID based on claim type.
        services.AddSingleton<IUserIdProvider, IdBasedUserIdProvider>();

        // next, we need to configure our signalR service.
        ISignalRServerBuilder signalRServiceBuilder = services.AddSignalR(hubOptions =>
        {
            // set up our hub options as well.
            hubOptions.MaximumReceiveMessageSize = long.MaxValue;       // the max size a message can be
            hubOptions.EnableDetailedErrors = true;                     // enable the detailed errors.
            hubOptions.MaximumParallelInvocationsPerClient = 10;        // the max parallel invocations per client should be 10
            hubOptions.StreamBufferCapacity = 200;                      // the stream buffer capacity should be 200

            // hubOptions.AddFilter<SignalRLimitFilter>();              // ignore adding a signalR limit filter.
        })
        .AddMessagePackProtocol(opt => // add a message pack protocol to the signalR so it knows the formats of the Dto's we are sending
        {
            // and create a composite resolver for the message pack serializer options
            IFormatterResolver resolver = CompositeResolver.Create(StandardResolverAllowPrivate.Instance,
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

            // also set our serializer options to the standard message pack serializer options
            opt.SerializerOptions = MessagePackSerializerOptions.Standard
                .WithCompression(MessagePackCompression.Lz4Block)
                .WithResolver(resolver);
        });

        // lets now configure our redi's connection for the signalR connection
        string redisConnection = gagspeakConfig.GetValue(nameof(ServerConfiguration.RedisConnectionString), string.Empty);
        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            logger.LogError("Redis connection string is missing or empty in the configuration.");
            throw new InvalidOperationException("Redis connection string must be provided.");
        }
        // append the redi's connection to the signalR service builder
        signalRServiceBuilder.AddStackExchangeRedis(redisConnection, options => { });

        // fetch the options from the redi's connection
        ConfigurationOptions options = ConfigurationOptions.Parse(redisConnection);

        // set the endpoint to the redi's connection endpoint
        EndPoint endpoint = options.EndPoints[0];
        // initialize a blank address and port
        string address = "";
        int port = 0;
        // if the endpoint is a DNS endpoint, set the address and port to the DNS endpoint host and port
        if (endpoint is DnsEndPoint dnsEndPoint)
        {
            address = dnsEndPoint.Host; port = dnsEndPoint.Port;
            logger.LogInformation("Redis Connection: {address}:{port}", address, port);
        }

        // otherwise, set the address and port to the IPEndpoint address and port
        if (endpoint is IPEndPoint ipEndPoint)
        { 
            address = ipEndPoint.Address.ToString(); port = ipEndPoint.Port;
            logger.LogInformation("Redis Connection: {address}:{port}", address, port);
        }

        // finally, configure the redi's connection for the signalR service
        RedisConfiguration redisConfiguration = new RedisConfiguration()
        {
            AbortOnConnectFail = true,                              // abort on connect failure
            KeyPrefix = "",                                         // set the key prefix to blank
            Hosts = new RedisHost[]                                 // set the hosts to the redi's connection host and port
            {
                new RedisHost(){ Host = address, Port = port },
            },
            AllowAdmin = true,                                      // allow admin
            ConnectTimeout = options.ConnectTimeout,                // set the connect timeout to the options connect timeout
            Database = 0,                                           // set the database to 0, which is the default database
            Ssl = false,                                            // do not require SSL
            Password = options.Password,                            // set the password to the options password

            ServerEnumerationStrategy = new ServerEnumerationStrategy() // declare the server enumeration statregy
            {
                Mode = ServerEnumerationStrategy.ModeOptions.All,
                TargetRole = ServerEnumerationStrategy.TargetRoleOptions.Any,
                UnreachableServerAction = ServerEnumerationStrategy.UnreachableServerActionOptions.Throw,
            },
            MaxValueLength = 1024,                                                  // declare the max value length
            PoolSize = gagspeakConfig.GetValue(nameof(ServerConfiguration.RedisPool), 50), // the max pool size
            SyncTimeout = options.SyncTimeout,                                      // and the sync timeout.
        };

        // now we can finally add the redi's configuration to our service collection.
        services.AddStackExchangeRedisExtensions<SystemTextJsonSerializer>(redisConfiguration);
    }

    /// <summary> Helper method for configuring the authorization module. 
    /// <para> 
    /// Authorization is important because it determines what users can access what resources.
    /// </para>
    /// </summary>
    private static void ConfigureAuthorization(IServiceCollection services)
    {
        // add transient handlers for the user requirement handler, valid token requirement handler, and valid token hub requirement handler
        services.AddTransient<IAuthorizationHandler, UserRequirementHandler>();
        services.AddTransient<IAuthorizationHandler, ValidTokenRequirementHandler>();
        services.AddTransient<IAuthorizationHandler, ValidTokenHubRequirementHandler>();

        // add the options for the JWT bearer options to the services.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            // configure the options for the JWT bearer options to validate the issuer, lifetime, audience, and issuer signing key
            .Configure<IConfigurationService<GagspeakConfigurationBase>>((options, config) =>
            {
                // set the token validation parameters to the following
                options.TokenValidationParameters = new()
                {
                    ValidateIssuer = false,             // this means we do not validate the issuer
                    ValidateLifetime = true,            // this means we validate the lifetime
                    ValidateAudience = false,           // this means we do not validate the audience which are the users
                    ValidateIssuerSigningKey = true,    // this means we validate the issuer signing key, which is the JWT
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(config.GetValue<string>(nameof(GagspeakConfigurationBase.Jwt)))), // and this defines how the JWT is signed
                };
            });

        // add the authentication services to the services, and configure the default authentication scheme to the JWT bearer defaults
        services.AddAuthentication(o =>
        {
            o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer();

        // Add the authorization services to the gagspeak servers.
        // Policies in this context are like the rules of your club. They define who gets to access which area.
        services.AddAuthorization(options =>
        {
            // This is the basic rule applied to everyone.
            // It requires that any user must be authenticated using a JWT (JSON Web Token) to access most areas.
            // Think of it as the basic club membership card.
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser().Build();

            // New TemporaryAccess policy
            // This policy requires the presence of a specific claim, "TemporaryAccess", set to "true".
            // It's like a temporary pass for special cases, allowing limited access for a short period.
            options.AddPolicy("TemporaryAccess", policy =>
            {
                policy.AddRequirements(new UserRequirement(UserRequirements.TemporaryAccess));
            });

            // Requires a valid JWT token, as enforced by the validTokenRequirement
            // and handled by the ValidTokenRequirementHandler
            options.AddPolicy("Authenticated", policy =>
            {
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new ValidTokenRequirement());
            });

            //  In addition to being authenticated, this requires that the spesific user claims that signify the
            //  user has been identified, and enforced by UserRequirement, and handled by UserRequirementHandler
            options.AddPolicy("Identified", policy =>
            {
                policy.AddRequirements(new UserRequirement(UserRequirements.Identified));
                policy.AddRequirements(new ValidTokenRequirement());

            });

            // These are higher-level rules for users who have roles with more privileges, like being a club manager (Admin)
            // or a security officer (Moderator).
            // They need to meet all the requirements of being identified and also have special roles.
            options.AddPolicy("Admin", policy =>
            {
                policy.AddRequirements(new UserRequirement(UserRequirements.Identified | UserRequirements.Administrator));
                policy.AddRequirements(new ValidTokenRequirement());

            });
            options.AddPolicy("Moderator", policy =>
            {
                policy.AddRequirements(new UserRequirement(UserRequirements.Identified | UserRequirements.Moderator | UserRequirements.Administrator));
                policy.AddRequirements(new ValidTokenRequirement());
            });

            // This is a unique rule for internal systems or employees, requiring a specific claim to be present. It's like a backstage pass.
            options.AddPolicy("Internal", new AuthorizationPolicyBuilder().RequireClaim(GagspeakClaimTypes.Internal, "true").Build());
        });
    }

    /// <summary> Helper method for configuring the database. </summary>
    private void ConfigureDatabase(IServiceCollection services, IConfigurationSection gagspeakConfig)
    {
        // lets begin by adding the database context pool to the services.
        services.AddDbContextPool<GagspeakDbContext>(options =>
        {   // be sure we use the default connection string from the configuration with npgsql
            options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"), builder =>
            {
                // set the migrations history table to _efmigrationshistory and the schema to public, using snake case naming convention
                builder.MigrationsHistoryTable("_efmigrationshistory", "public");
                builder.MigrationsAssembly("GagSpeakShared");
            }).UseSnakeCaseNamingConvention();

            options.EnableThreadSafetyChecks(false); // do not enable thread safety checks
        }, gagspeakConfig.GetValue(nameof(GagspeakConfigurationBase.DbContextPoolSize), 1024)); // set the pool size to the configuration value

        // now we should add the database context factory to the service collection
        services.AddDbContextFactory<GagspeakDbContext>(options =>
        {
            // this factory will use all the same settings that we defined above.
            options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"), builder =>
            {
                builder.MigrationsHistoryTable("_efmigrationshistory", "public");
                builder.MigrationsAssembly("GagSpeakShared");
            }).UseSnakeCaseNamingConvention();
            options.EnableThreadSafetyChecks(false);
        });
    }

    /// <summary> The Configure method is responsible for configuring the application's request pipeline. </summary>
    /// <param name="app"> The application builder for the server. </param>
    /// <param name="env"> The web host environment for the server. </param>
    /// <param name="logger"> The logger for the startup class. </param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
    {
        logger.LogInformation("Running Configure");
        // Fetch the IconfigService for the gagspeak config base.
        IConfigurationService<GagspeakConfigurationBase> config = app.ApplicationServices.GetRequiredService<IConfigurationService<GagspeakConfigurationBase>>();

        // use routing for our endpoint connections
        app.UseRouting();

        // use websocket connections
        app.UseWebSockets();
        app.UseHttpMetrics();

        // for the metrics server, initialize it with the metrics port from the configuration
#pragma warning disable IDISP001 // Dispose created
        KestrelMetricServer metricServer = new KestrelMetricServer(config.GetValueOrDefault<int>(nameof(GagspeakConfigurationBase.MetricsPort), 4980));
#pragma warning restore IDISP001 // Dispose created
#pragma warning disable IDISP004 // Don't ignore created IDisposable
        metricServer.Start();
#pragma warning restore IDISP004 // Don't ignore created IDisposable

        // have authentication and authorization for the server
        app.UseAuthentication();
        app.UseAuthorization();

        // configure the endpoints for the server
        app.UseEndpoints(endpoints =>
        {
            // create a maphub that maps the gagspeak hub to the gagspeak hub path
            endpoints.MapHub<GagspeakHub>(IGagspeakHub.Path, options =>
            {
                options.ApplicationMaxBufferSize = 5242880; // the max buffer size
                options.TransportMaxBufferSize = 5242880;   // the transport max buffer size
                // configure the transports to be websockets, server sent events, and long polling
                options.Transports = HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling;
            });

            // map the health checks to the health endpoint
            endpoints.MapHealthChecks("/health").AllowAnonymous(); // allow anonymous access to the health checks
            endpoints.MapControllers();

            // log the endpoints for the server if they are not null
            foreach (RouteEndpoint source in endpoints.DataSources.SelectMany(e => e.Endpoints).Cast<RouteEndpoint>())
            {
                if (source is null) continue;
                _logger.LogInformation("Endpoint: {url} ", source.RoutePattern.RawText);
                // log the full path including the base address

            }
        });

    }
}
