using GagspeakAuthentication.Controllers;
using GagspeakAuthentication.Services;
using GagspeakShared.Data;
using GagspeakShared.Metrics;
using GagspeakShared.RequirementHandlers;
using GagspeakShared.Services;
using GagspeakShared.Utils;
using GagspeakShared.Utils.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prometheus;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.System.Text.Json;
using System.Net;
using System.Text;

namespace GagspeakAuthentication;

public class Startup
{
    private readonly IConfiguration _config;
    private ILogger<Startup> _logger;

    public Startup(IConfiguration configuration, ILogger<Startup> logger)
    {
        _config = configuration;
        _logger = logger;
    }

    /// <summary> The Configure method for our app enviorment. </summary>
    /// <param name="app"> The application builder for the gagspeak authentication server. </param>
    /// <param name="env"> The web host enviorment for the gagspeak authentication server. </param>
    /// <param name="logger"> The logger for the startup class. </param>
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
    {
        // get the configuration service from the config base class
        var config = app.ApplicationServices.GetRequiredService<IConfigurationService<GagspeakConfigurationBase>>();
        // configure the application to use routing for the authentications
        app.UseRouting();
        // use the http metrics so we can guage wtf is going on with the server
        app.UseHttpMetrics();
        // use authentication and authorization to help with security and user identification
        app.UseAuthentication();
        app.UseAuthorization();

        // next, set up the metrics server at the port specified in the config from appsettings.json.
        // This should be different from the other components ports.
        var metricServer = new KestrelMetricServer(config.GetValueOrDefault<int>(nameof(GagspeakConfigurationBase.MetricsPort), 6152));
        // start the metrics server
        metricServer.Start();

        // set up the endpoints for the application. Because this is for authentications,
        // we have lots of various endpoints based on the user using it.
        app.UseEndpoints(endpoints =>
        {
            // so make sure that not only do we map the controllers for our authentication
            endpoints.MapControllers();

            // but also that for each source in the datasources of our endpoints, we log the url of the endpoint.
            foreach (var source in endpoints.DataSources.SelectMany(e => e.Endpoints).Cast<RouteEndpoint>())
            {
                // if the source is not null
                if (source == null) continue;
                // we should log the enpoints.
                _logger.LogInformation("Endpoint: {url} ", source.RoutePattern.RawText);
            }
        });
    }

    /// <summary> The configure services method for the gagspeak authentication server. </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        // first, lets set up the configuration section for our gagspeak authentication server.
        // (This is different from above, see we used IConfigurationService over IConfigSection
        IConfigurationSection gagspeakConfig = _config.GetRequiredSection("GagSpeak");

        // append the http context accessor to the services
        services.AddHttpContextAccessor();

        // configure redi's for our authentication server.
        ConfigureRedis(services, gagspeakConfig);

        services.AddSingleton<SecretKeyAuthenticatorService>();

        // Amend the configuration services for the gagspeak authentication server
        services.Configure<AuthServiceConfiguration>(_config.GetRequiredSection("GagSpeak"));
        services.Configure<GagspeakConfigurationBase>(_config.GetRequiredSection("GagSpeak"));

        // append the servers token generator so we can properly generate tokens.
        services.AddSingleton<ServerTokenGenerator>();

        // configure the authorization for our authentication server
        ConfigureAuthorization(services);

        // configure the database for our services
        ConfigureDatabase(services, gagspeakConfig);

        // configure our config services for the gagspeak authentication server
        ConfigureConfigServices(services);

        // configure the metrics data
        ConfigureMetrics(services);

        // append the health checks
        services.AddHealthChecks();

        // finally, add controllers so we can scope our access to the authentication component of the gagspeak server.
        services.AddControllers().ConfigureApplicationPartManager(a =>
        {
            // remove the default feature provider
            a.FeatureProviders.Remove(a.FeatureProviders.OfType<ControllerFeatureProvider>().First());
            // then append our own with a limited scope of our JwtController.
            a.FeatureProviders.Add(new AllowedControllersFeatureProvider(typeof(JwtController)));
        });
    }

    /// <summary> Helper method to configure the authorization for the gagspeak authentication server. </summary>
    private static void ConfigureAuthorization(IServiceCollection services)
    {
        // add the user and valid token requirement handlers.
        services.AddTransient<IAuthorizationHandler, UserRequirementHandler>();
        services.AddTransient<IAuthorizationHandler, ValidTokenRequirementHandler>();

        // add the jwt bearer options to the services
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            // configure the authentication scheme format
            .Configure<IConfigurationService<GagspeakConfigurationBase>>((options, config) =>
            {
                // to have the options for valid parmeters set up
                options.TokenValidationParameters = new()
                {
                    ValidateIssuer = false, // we do not need to validate the issuer
                    ValidateLifetime = true, // but we do need to validate the lifetime of the token
                    ValidateAudience = false, // we do not need to validate the audience
                    ValidateIssuerSigningKey = true, // but we do need to validate the issuer signing key
                    // and we need to set the issuerSigningKey to the jwt key from the config
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.ASCII.GetBytes(config.GetValue<string>(nameof(GagspeakConfigurationBase.Jwt)))),
                };
            });

        // add the authentication to the services with the default bearer scheme
        services.AddAuthentication(o =>
        {
            o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer();

        // add the authorization options to the services
        services.AddAuthorization(options =>
        {
            // set the default policy to require an authenticated user
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser().Build();

            // next, lets add our policies for different states people can have.
            // New TemporaryAccess policy
            options.AddPolicy("TemporaryAccess", policy =>
            {
                policy.AddRequirements(new UserRequirement(UserRequirements.TemporaryAccess));
            });

            // to be authenticated, you must satisfy the policy criteria stating that
            options.AddPolicy("Authenticated", policy =>
            {
                // you must have a valid token
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                // and you must be an authenticated user
                policy.RequireAuthenticatedUser();
                // and you must satisfy the valid token requirement
                policy.AddRequirements(new ValidTokenRequirement());
            });
            // to be identified, you must
            options.AddPolicy("Identified", policy =>
            {
                // satisfy the user requirement
                policy.AddRequirements(new UserRequirement(UserRequirements.Identified));
                // and satisfy the valid token requirement
                policy.AddRequirements(new ValidTokenRequirement());

            });
            // to be an admin, you must
            options.AddPolicy("Admin", policy =>
            {
                // satisfy the user requirement for identified and administrator
                policy.AddRequirements(new UserRequirement(UserRequirements.Identified | UserRequirements.Administrator));
                // and satisfy the valid token requirement
                policy.AddRequirements(new ValidTokenRequirement());

            });
            // to be a moderator, you must
            options.AddPolicy("Moderator", policy =>
            {
                // satisfy the user requirement for identified and moderator
                policy.AddRequirements(new UserRequirement(UserRequirements.Identified | UserRequirements.Moderator | UserRequirements.Administrator));
                // and satisfy the valid token requirement
                policy.AddRequirements(new ValidTokenRequirement());
            });
            // to be internal, you must satisfy the internal claim requirement for the token to be true (internal)
            options.AddPolicy("Internal", new AuthorizationPolicyBuilder().RequireClaim(GagspeakClaimTypes.Internal, "true").Build());
        });
    }

    /// <summary> Helper method to configure the Gagspeak Metrics for the gagspeak authentication server. </summary>
    private static void ConfigureMetrics(IServiceCollection services)
    {
        // add to the service collection the gagspeak metrics for the authentication server
        services.AddSingleton<GagspeakMetrics>(m => new GagspeakMetrics(m.GetService<ILogger<GagspeakMetrics>>(), new List<string>
        {
            MetricsAPI.CounterAuthenticationCacheHits,      // the counter for the number of cache hits
            MetricsAPI.CounterAuthenticationFailures,       // the counter for the number of authentication failures
            MetricsAPI.CounterAuthenticationRequests,       // the counter for the number of authentication requests
            MetricsAPI.CounterAuthenticationSuccesses,      // the counter for the number of authentication successes
        }, new List<string>
        {
            MetricsAPI.GaugeAuthenticationCacheEntries,     // the gauge for the number of cache entries
        }));
    }

    /// <summary> Helper method to configure the redi's for the authentication server component of the gagspeak server. </summary>
    private static void ConfigureRedis(IServiceCollection services, IConfigurationSection gagspeakConfig)
    {
        // define the redi's connection string as the string provided in the serverConfig
        var redisConnection = gagspeakConfig.GetValue(nameof(ServerConfiguration.RedisConnectionString), string.Empty);
        // fetch the options from parsing out the redi's connection
        var options = ConfigurationOptions.Parse(redisConnection);

        // get the endpoint from the options
        var endpoint = options.EndPoints[0];
        // set the address to an empty string for now and the port to 0.
        string address = "";
        int port = 0;
        // check if the endpoint is dnsenpoint. If it is,set address to the dnsEndpoint.Host, and port to the dnsEndpoint.Port.
        if (endpoint is DnsEndPoint dnsEndPoint) { address = dnsEndPoint.Host; port = dnsEndPoint.Port; }
        // otherwise, if it is an IP endpoint, then set the address to the ipEndpoint.Address.ToString(), and the port to the ipEndpoint.Port.
        if (endpoint is IPEndPoint ipEndPoint) { address = ipEndPoint.Address.ToString(); port = ipEndPoint.Port; }

        // set up the redi's configuration for the server
        var redisConfiguration = new RedisConfiguration()
        {
            AbortOnConnectFail = false,  // abort redi's connection on failure.
            KeyPrefix = "", // clear the key prefix to be blank.
            Hosts = new RedisHost[] // set the new Redi's host to the address and port.
            {
                new RedisHost(){ Host = address, Port = port },
            },
            AllowAdmin = true, // allow the admin to have access to the redi's server.
            ConnectTimeout = options.ConnectTimeout, // set the connection timeout duration to the timeout from the options.
            Database = 0,   // set the database to 0. (none)
            Ssl = false,    // do not require ssl
            Password = options.Password, // set the password to the password from the options.
            ServerEnumerationStrategy = new ServerEnumerationStrategy() // define the server enumeration strategy
            {
                Mode = ServerEnumerationStrategy.ModeOptions.All, // set the mode to all
                TargetRole = ServerEnumerationStrategy.TargetRoleOptions.Any, // set the target role to any
                UnreachableServerAction = ServerEnumerationStrategy.UnreachableServerActionOptions.Throw, // throw an exception if the server is unreachable
            },
            MaxValueLength = 1024, // max val length (huh?)
            PoolSize = gagspeakConfig.GetValue(nameof(ServerConfiguration.RedisPool), 50), // set the number of connections in the pool to the value in the config, or 50.
            SyncTimeout = options.SyncTimeout, // determine the sync timeout for redi's.
            LoggerFactory = services.BuildServiceProvider().GetRequiredService<ILoggerFactory>(), // get the logger factory from the services.
        };

        // add the redi's extensions to the services with the system text json serializer.
        services.AddStackExchangeRedisExtensions<SystemTextJsonSerializer>(redisConfiguration);
    }

    /// <summary> Helper method to configure the config services for the gagspeak authentication server. 
    /// <para> It will configure the AuthServiceConfiguration && GagspeakConfigurationBase </para>
    /// </summary>
    private void ConfigureConfigServices(IServiceCollection services)
    {
        services.AddSingleton<IConfigurationService<AuthServiceConfiguration>, GagspeakConfigServiceServer<AuthServiceConfiguration>>();
        services.AddSingleton<IConfigurationService<GagspeakConfigurationBase>, GagspeakConfigServiceServer<GagspeakConfigurationBase>>();
    }

    /// <summary> Helper method to configure the database for our authentication service so they can be linked. </summary>
    private void ConfigureDatabase(IServiceCollection services, IConfigurationSection gagspeakConfig)
    {
        // add the gagspeak database context to our services, set up with customized options.
        services.AddDbContextPool<GagspeakDbContext>(options =>
        {
            // use postgresql as the database provider, with the connection string from the appsettings.json file.
            options.UseNpgsql(_config.GetConnectionString("DefaultConnection"), builder =>
            {
                // set the builders migrations history table to ef migrations history in the public schema.
                builder.MigrationsHistoryTable("_efmigrationshistory", "public");
                // and migrate the assembly from GagspeakShared (where the GagspeakDbContext is located)
                builder.MigrationsAssembly("GagspeakShared");
                // use the snakeCaseNamingConvention
            }).UseSnakeCaseNamingConvention();
            // do not include thread safety checks
            options.EnableThreadSafetyChecks(false);
            // and set the pool size to the value in the configBase, or 1024
        }, gagspeakConfig.GetValue(nameof(GagspeakConfigurationBase.DbContextPoolSize), 1024));

        // finally, add the gagspeak dbcontext FACTORY (different from pool)
        services.AddDbContextFactory<GagspeakDbContext>(options =>
        {
            // use the same connectionstring as above)
            options.UseNpgsql(_config.GetConnectionString("DefaultConnection"), builder =>
            {
                // (with the same options as above)
                builder.MigrationsHistoryTable("_efmigrationshistory", "public");
                builder.MigrationsAssembly("GagspeakShared");
            }).UseSnakeCaseNamingConvention();
            options.EnableThreadSafetyChecks(false);
        });
    }
}
