using Microsoft.EntityFrameworkCore;
using GagSpeakServer.Hubs;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using AspNetCoreRateLimit;
using GagSpeakShared.Data;
using GagSpeakShared.Metrics;
using GagSpeakServer.Services;
using GagSpeakShared.Utils;
using GagSpeakShared.Services;
using Prometheus;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Configuration;
using System.Net;
using StackExchange.Redis.Extensions.System.Text.Json;
using GagSpeak.API.SignalR;
using MessagePack;
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Mvc.Controllers;
using GagSpeakServer.Controllers;
using GagSpeakShared.RequirementHandlers;
using GagSpeakShared.Utils.Configuration;

namespace GagSpeakServer;

public class Startup
{
    private readonly ILogger<Startup> _logger;

    public Startup(IConfiguration configuration, ILogger<Startup> logger)
    {
        Configuration = configuration;
        _logger = logger;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddTransient(_ => Configuration);

        var GagSpeakConfig = Configuration.GetRequiredSection("GagSpeak");

        // configure metrics
        ConfigureMetrics(services);

        // configure database
        ConfigureDatabase(services, GagSpeakConfig);

        // configure authentication and authorization
        ConfigureAuthorization(services);

        // configure rate limiting
        ConfigureIpRateLimiting(services);

        // configure SignalR
        ConfigureSignalR(services, GagSpeakConfig);

        // configure GagSpeak specific services
        ConfigureGagSpeakServices(services, GagSpeakConfig);

        services.AddHealthChecks();
        services.AddControllers().ConfigureApplicationPartManager(a =>
        {
            a.FeatureProviders.Remove(a.FeatureProviders.OfType<ControllerFeatureProvider>().First());
            if (GagSpeakConfig.GetValue<Uri>(nameof(ServerConfiguration.MainServerAddress), defaultValue: null) == null)
            {
                a.FeatureProviders.Add(new AllowedControllersFeatureProvider(typeof(GagSpeakServerConfigurationController), typeof(GagSpeakBaseConfigurationController), typeof(ClientMessageController)));
            }
            else
            {
                a.FeatureProviders.Add(new AllowedControllersFeatureProvider());
            }
        });
    }

    private void ConfigureGagSpeakServices(IServiceCollection services, IConfigurationSection GagSpeakConfig)
    {
        bool isMainServer = GagSpeakConfig.GetValue<Uri>(nameof(ServerConfiguration.MainServerAddress), defaultValue: null) == null;

        services.Configure<ServerConfiguration>(Configuration.GetRequiredSection("GagSpeak"));
        services.Configure<GagSpeakConfigurationBase>(Configuration.GetRequiredSection("GagSpeak"));

        services.AddSingleton<ServerTokenGenerator>();
        services.AddSingleton<SystemInfoService>();
        services.AddSingleton<OnlineSyncedPairCacheService>();
        services.AddHostedService(provider => provider.GetService<SystemInfoService>());
        // configure services based on main server status
        ConfigureServicesBasedOnShardType(services, GagSpeakConfig, isMainServer);

        services.AddSingleton(s => new GagSpeakCensus(s.GetRequiredService<ILogger<GagSpeakCensus>>(), GagSpeakConfig.GetValue("XIVAPIKey", string.Empty)));
        services.AddHostedService(p => p.GetRequiredService<GagSpeakCensus>());

        if (isMainServer)
        {
            services.AddSingleton<UserCleanupService>();
            services.AddHostedService(provider => provider.GetService<UserCleanupService>());
        }
    }

    private static void ConfigureSignalR(IServiceCollection services, IConfigurationSection GagSpeakConfig)
    {
        services.AddSingleton<IUserIdProvider, IdBasedUserIdProvider>();

        var signalRServiceBuilder = services.AddSignalR(hubOptions =>
        {
            hubOptions.MaximumReceiveMessageSize = long.MaxValue;
            hubOptions.EnableDetailedErrors = true;
            hubOptions.MaximumParallelInvocationsPerClient = 10;
            hubOptions.StreamBufferCapacity = 200;

            hubOptions.AddFilter<SignalRLimitFilter>();
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


        // configure redis for SignalR
        var redisConnection = GagSpeakConfig.GetValue(nameof(ServerConfiguration.RedisConnectionString), string.Empty);
        signalRServiceBuilder.AddStackExchangeRedis(redisConnection, options => { });

        var options = ConfigurationOptions.Parse(redisConnection);

        var endpoint = options.EndPoints[0];
        string address = "";
        int port = 0;
        if (endpoint is DnsEndPoint dnsEndPoint) { address = dnsEndPoint.Host; port = dnsEndPoint.Port; }
        if (endpoint is IPEndPoint ipEndPoint) { address = ipEndPoint.Address.ToString(); port = ipEndPoint.Port; }
        var redisConfiguration = new RedisConfiguration()
        {
            AbortOnConnectFail = true,
            KeyPrefix = "",
            Hosts = new RedisHost[]
            {
                new RedisHost(){ Host = address, Port = port },
            },
            AllowAdmin = true,
            ConnectTimeout = options.ConnectTimeout,
            Database = 0,
            Ssl = false,
            Password = options.Password,
            ServerEnumerationStrategy = new ServerEnumerationStrategy()
            {
                Mode = ServerEnumerationStrategy.ModeOptions.All,
                TargetRole = ServerEnumerationStrategy.TargetRoleOptions.Any,
                UnreachableServerAction = ServerEnumerationStrategy.UnreachableServerActionOptions.Throw,
            },
            MaxValueLength = 1024,
            PoolSize = GagSpeakConfig.GetValue(nameof(ServerConfiguration.RedisPool), 50),
            SyncTimeout = options.SyncTimeout,
        };

        services.AddStackExchangeRedisExtensions<SystemTextJsonSerializer>(redisConfiguration);
    }

    private void ConfigureIpRateLimiting(IServiceCollection services)
    {
        services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
        services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        services.AddMemoryCache();
        services.AddInMemoryRateLimiting();
    }

    private static void ConfigureAuthorization(IServiceCollection services)
    {
        services.AddTransient<IAuthorizationHandler, UserRequirementHandler>();
        services.AddTransient<IAuthorizationHandler, ValidTokenRequirementHandler>();
        services.AddTransient<IAuthorizationHandler, ValidTokenHubRequirementHandler>();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IConfigurationService<GagSpeakConfigurationBase>>((options, config) =>
            {
                options.TokenValidationParameters = new()
                {
                    ValidateIssuer = false,
                    ValidateLifetime = true,
                    ValidateAudience = false,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(config.GetValue<string>(nameof(GagSpeakConfigurationBase.Jwt)))),
                };
            });

        services.AddAuthentication(o =>
        {
            o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer();

        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser().Build();
            options.AddPolicy("Authenticated", policy =>
            {
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new ValidTokenRequirement());
            });
            options.AddPolicy("Identified", policy =>
            {
                policy.AddRequirements(new UserRequirement(UserRequirements.Identified));
                policy.AddRequirements(new ValidTokenRequirement());

            });
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
            options.AddPolicy("Internal", new AuthorizationPolicyBuilder().RequireClaim(GagSpeakClaimTypes.Internal, "true").Build());
        });
    }

    private void ConfigureDatabase(IServiceCollection services, IConfigurationSection GagSpeakConfig)
    {
        services.AddDbContextPool<GagSpeakDbContext>(options =>
        {
            options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"), builder =>
            {
                builder.MigrationsHistoryTable("_efmigrationshistory", "public");
                builder.MigrationsAssembly("GagSpeakShared");
            }).UseSnakeCaseNamingConvention();
            options.EnableThreadSafetyChecks(false);
        }, GagSpeakConfig.GetValue(nameof(GagSpeakConfigurationBase.DbContextPoolSize), 1024));
        services.AddDbContextFactory<GagSpeakDbContext>(options =>
        {
            options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection"), builder =>
            {
                builder.MigrationsHistoryTable("_efmigrationshistory", "public");
                builder.MigrationsAssembly("GagSpeakShared");
            }).UseSnakeCaseNamingConvention();
            options.EnableThreadSafetyChecks(false);
        });
    }

    private static void ConfigureMetrics(IServiceCollection services)
    {
        services.AddSingleton<GagSpeakMetrics>(m => new GagSpeakMetrics(m.GetService<ILogger<GagSpeakMetrics>>(), new List<string>
        {
            MetricsAPI.CounterInitializedConnections,
            MetricsAPI.CounterUserPushData,
            MetricsAPI.CounterUserPushDataTo,
            MetricsAPI.CounterUsersRegisteredDeleted,
            MetricsAPI.CounterAuthenticationCacheHits,
            MetricsAPI.CounterAuthenticationFailures,
            MetricsAPI.CounterAuthenticationRequests,
            MetricsAPI.CounterAuthenticationSuccesses,
            MetricsAPI.CounterUserPairCacheHit,
            MetricsAPI.CounterUserPairCacheMiss,
            MetricsAPI.CounterUserPairCacheNewEntries,
            MetricsAPI.CounterUserPairCacheUpdatedEntries,
        }, new List<string>
        {
            MetricsAPI.GaugeAuthorizedConnections,
            MetricsAPI.GaugeConnections,
            MetricsAPI.GaugePairs,
            MetricsAPI.GaugePairsPaused,
            MetricsAPI.GaugeAvailableIOWorkerThreads,
            MetricsAPI.GaugeAvailableWorkerThreads,
            MetricsAPI.GaugeGroups,
            MetricsAPI.GaugeGroupPairs,
            MetricsAPI.GaugeUsersRegistered,
            MetricsAPI.GaugeAuthenticationCacheEntries,
            MetricsAPI.GaugeUserPairCacheEntries,
            MetricsAPI.GaugeUserPairCacheUsers,
        }));
    }

    private static void ConfigureServicesBasedOnShardType(IServiceCollection services, IConfigurationSection GagSpeakConfig, bool isMainServer)
    {
        if (!isMainServer)
        {
            services.AddSingleton<IConfigurationService<ServerConfiguration>, GagSpeakConfigurationServiceClient<ServerConfiguration>>();
            services.AddSingleton<IConfigurationService<GagSpeakConfigurationBase>, GagSpeakConfigurationServiceClient<GagSpeakConfigurationBase>>();

            services.AddHostedService(p => (GagSpeakConfigurationServiceClient<ServerConfiguration>)p.GetService<IConfigurationService<ServerConfiguration>>());
            services.AddHostedService(p => (GagSpeakConfigurationServiceClient<GagSpeakConfigurationBase>)p.GetService<IConfigurationService<GagSpeakConfigurationBase>>());
        }
        else
        {
            services.AddSingleton<IConfigurationService<ServerConfiguration>, GagSpeakConfigurationServiceServer<ServerConfiguration>>();
            services.AddSingleton<IConfigurationService<GagSpeakConfigurationBase>, GagSpeakConfigurationServiceServer<GagSpeakConfigurationBase>>();
        }
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
    {
        logger.LogInformation("Running Configure");

        var config = app.ApplicationServices.GetRequiredService<IConfigurationService<GagSpeakConfigurationBase>>();

        app.UseIpRateLimiting();

        app.UseRouting();

        app.UseWebSockets();
        app.UseHttpMetrics();

        var metricServer = new KestrelMetricServer(config.GetValueOrDefault<int>(nameof(GagSpeakConfigurationBase.MetricsPort), 4980));
        metricServer.Start();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<GagSpeakHub>(IGagSpeakHub.Path, options =>
            {
                options.ApplicationMaxBufferSize = 5242880;
                options.TransportMaxBufferSize = 5242880;
                options.Transports = HttpTransportType.WebSockets | HttpTransportType.ServerSentEvents | HttpTransportType.LongPolling;
            });

            endpoints.MapHealthChecks("/health").AllowAnonymous();
            endpoints.MapControllers();

            foreach (var source in endpoints.DataSources.SelectMany(e => e.Endpoints).Cast<RouteEndpoint>())
            {
                if (source == null) continue;
                _logger.LogInformation("Endpoint: {url} ", source.RoutePattern.RawText);
            }
        });

    }
}
