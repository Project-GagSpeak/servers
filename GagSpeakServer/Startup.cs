using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GagspeakServer.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GagspeakServer.Hubs;

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
        // create a mare config in our configuration under a section we define
        var gagspeakConfig = Configuration.GetSection("Gagspeak");

        // we need to fill our service collection container with the services we need, so we let's add them

        ConfigureDatabase(services, gagspeakConfig);

        ConfigureSignalR(services, gagspeakConfig);
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
        });
    }

    private void ConfigureDatabase(IServiceCollection services, IConfigurationSection gagspeakConfig)
    {
        services.AddDbContext<GagspeakDbContext>();
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
            // see the heartbeat of the server
            endpoints.MapHub<Connection>("/heartbeat");
            // see the user
            // endpoints.MapHub<User>("/user");
            // link up the chat hub
            endpoints.MapHub<ChatHub>("/chathub");
        });
    }
}
