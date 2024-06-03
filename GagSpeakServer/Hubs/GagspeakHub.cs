using Gagspeak.API.SignalR;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Gagspeak.API.Data;
using Gagspeak.API.Data.Enum;
using Gagspeak.API.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using GagspeakServer.Data;
using GagspeakServer.Utils;
using GagspeakServer.Services;
using GagspeakServer.Utils.Configuration;

namespace GagspeakServer.Hubs;
public partial class GagspeakHub : Hub<IGagspeakHub>, IGagspeakHub
{
   // A thread-safe dictionary to store user connections
    private static readonly ConcurrentDictionary<string, string> _userConnections = new(StringComparer.Ordinal);  
    
    // Service for getting system information
    private readonly SystemInfoService _systemInfoService;
    
    // Accessor to get HTTP context information
    private readonly IHttpContextAccessor _contextAccessor;
    
    // Logger specific to GagspeakHub
    private readonly GagspeakHubLogger _logger;
    
    // Address of the file server
    private readonly Uri _fileServerAddress;

    // Expected version of the client
    private readonly Version _expectedClientVersion;
    
    // Lazy initialization of GagSpeakDbContext
    private readonly Lazy<GagspeakDbContext> _dbContextLazy;
    
    // Property to get the GagSpeakDbContext
    private GagspeakDbContext DbContext => _dbContextLazy.Value;

    // Constructor for GagspeakHub
    public GagspeakHub(IDbContextFactory<GagspeakDbContext> GagSpeakDbContextFactory,
        SystemInfoService systemInfoService, IConfigService<ServerConfiguration> configuration,
        IHttpContextAccessor contextAccessor, ILogger<GagspeakHub> logger)
    {
        _systemInfoService = systemInfoService;
        _fileServerAddress = configuration.GetValue<Uri>(nameof(ServerConfiguration.CdnFullUrl));
        _contextAccessor = contextAccessor;
        _expectedClientVersion = configuration.GetValueOrDefault(nameof(ServerConfiguration.ExpectedClientVersion), new Version(0, 0, 0));
        _logger = new GagspeakHubLogger(this, logger);
        _dbContextLazy = new Lazy<GagspeakDbContext>(() => GagSpeakDbContextFactory.CreateDbContext());
    }

    // for the disposal of the gagspeak of hubbs
    protected override void Dispose(bool disposing)
    {
        // if disposing is true
        if (disposing)
        {
            // and the lazy value is created, dispose of the database context
            if (_dbContextLazy.IsValueCreated) DbContext.Dispose();
        }
        // then dispose the base with true set
        base.Dispose(disposing);
    }


    [Authorize(Policy = "Identified")]
    public async Task<ConnectionDto> GetConnectionDto()
    {
        _logger.LogCallInfo();

        // await all clients to update the system information
        await Clients.Caller.Client_UpdateSystemInfo(_systemInfoService.SystemInfoDto).ConfigureAwait(false);

        // get the user from the database
        var dbUser = await DbContext.Users.SingleAsync(f => f.UID == UserUID).ConfigureAwait(false);
        
        // update their last logged in time to the current time in UTC
        dbUser.LastLoggedIn = DateTime.UtcNow;

        // await for the them to recieve the server message 
        await Clients.Caller.Client_RecieveServerMessage(MessageSeverity.Information, "Welcome to the Gagspeak Server, Current Online Users: " + _systemInfoService.SystemInfoDto.OnlineUsers).ConfigureAwait(false);

        await DbContext.SaveChangesAsync().ConfigureAwait(false);

        return new ConnectionDto(new UserData(dbUser.UID, string.IsNullOrWhiteSpace(dbUser.Alias) ? null : dbUser.Alias))
        {
            CurrentClientVersion = _expectedClientVersion,
            ServerVersion = IGagspeakHub.ApiVersion,
        };
    }

    [Authorize(Policy = "Authenticated")]
    public async Task<bool> CheckClientHealth()
    {
        Console.WriteLine("CheckingClientHealth");
        //await UpdateUserOnRedis().ConfigureAwait(false);

        return false;
    }

    [Authorize(Policy = "Authenticated")]
    public override async Task OnConnectedAsync()
    {
        // for on connected async, we need to do a few things...


        // first, if the user connections dictionary contains the user UID, get the old ID
        if (_userConnections.TryGetValue(UserUID, out var oldId))
        {
            // log a warning that the user is updating their ID
            _logger.LogCallWarning(GagspeakHubLogger.Args(_contextAccessor.GetIpAddress(), "UpdatingId", oldId, Context.ConnectionId));
            // then update the user connections dictionary with the new ID
            _userConnections[UserUID] = Context.ConnectionId;
        }
        // otherwise, if the user connections dictionary does not contain the user UID
        else
        {
            // try to
            try
            {
                // log the call info
                _logger.LogCallInfo(GagspeakHubLogger.Args(_contextAccessor.GetIpAddress(), Context.ConnectionId, UserCharaIdent));
                // initialize the player with the user UID
                _userConnections[UserUID] = Context.ConnectionId;
            }
            catch
            {
                _userConnections.Remove(UserUID, out _);
            }
        }

        // await the base onConnected to finish
        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    // when we go to disconnect, we need to do a few things...
    [Authorize(Policy = "Authenticated")]
    public override async Task OnDisconnectedAsync(Exception exception)
    {
        // first, if the user connections dictionary contains the user UID, get the connection ID
        if (_userConnections.TryGetValue(UserUID, out var connectionId)
            && string.Equals(connectionId, Context.ConnectionId, StringComparison.Ordinal))
        {
            // try to
            try
            {
                // log the call info
                _logger.LogCallInfo(GagspeakHubLogger.Args(_contextAccessor.GetIpAddress(), Context.ConnectionId, UserCharaIdent));
                if (exception != null)
                    _logger.LogCallWarning(GagspeakHubLogger.Args(_contextAccessor.GetIpAddress(), Context.ConnectionId, exception.Message, exception.StackTrace));

                // send that we just went offline to all paired users...

                // await SendOfflineToAllPairedUsers().ConfigureAwait(false);

                // make changes to the files or something but that shouldnt madder right now since we just want the basics for testing.
                // DbContext.RemoveRange(DbContext.Files.Where(f => !f.Uploaded && f.UploaderUID == UserUID));
                await DbContext.SaveChangesAsync().ConfigureAwait(false);

            }
            catch { }
            finally
            {
                _userConnections.Remove(UserUID, out _);
            }
        }
        else
        {
            _logger.LogCallWarning(GagspeakHubLogger.Args(_contextAccessor.GetIpAddress(), "ObsoleteId", UserUID, Context.ConnectionId));
        }

        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }
}

