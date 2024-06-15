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
using StackExchange.Redis.Extensions.Core.Abstractions;

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

    // Redis database for caching (Redi's allows us to have a simple way to store and manage user state with large scale)
    private readonly IRedisDatabase _redis;

    // Service for managing online synced pairs
    private readonly OnlineSyncedPairCacheService _onlineSyncedPairCacheService;

	// Expected version of the client
	private readonly Version _expectedClientVersion;
	
	// Lazy initialization of GagSpeakDbContext
	private readonly Lazy<GagspeakDbContext> _dbContextLazy;
	
	// Property to get the GagSpeakDbContext
	private GagspeakDbContext DbContext => _dbContextLazy.Value;

	// Constructor for GagspeakHub
	public GagspeakHub(IDbContextFactory<GagspeakDbContext> GagSpeakDbContextFactory,
		ILogger<GagspeakHub> logger, SystemInfoService systemInfoService, IRedisDatabase redis,
		IConfigService<ServerConfiguration> configuration, IHttpContextAccessor contextAccessor,
		OnlineSyncedPairCacheService onlineSyncedPairCacheService)
	{
		_systemInfoService = systemInfoService;
		_expectedClientVersion = configuration.GetValueOrDefault(nameof(ServerConfiguration.ExpectedClientVersion), new Version(0, 0, 0));
		_contextAccessor = contextAccessor;
		_redis = redis;
		_onlineSyncedPairCacheService = onlineSyncedPairCacheService;
		_logger = new GagspeakHubLogger(this, logger);
		_dbContextLazy = new Lazy<GagspeakDbContext>(() => GagSpeakDbContextFactory.CreateDbContext());
	}

	/// <summary> Disposes of the database context if created upon the GagSpeak hub's disposal.</summary>
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

	/// <summary> Called by a connected client when they want to request a ConnectionDto object from the server.
	/// <para>
	/// This method required the requesting client to have the authorize policy "Identified" (Meaning they have passed authorization)
	/// is bound to their request for the function to proceed. 
	/// </para>
	/// <para> This is called upon login (and maybe regular updates?) </para>
	/// </summary>
	[Authorize(Policy = "Identified")]
	public async Task<ConnectionDto> GetConnectionDto()
	{
		// log the caller who requested this method
		_logger.LogCallInfo();

		// return to the caller who made the request the SystemInfoDto of the server. (a seperate call to the client who made the request, not the ConnectionDto return)
		_logger.LogMessage("Updating System Info To client who called the request");
		await Clients.Caller.Client_UpdateSystemInfo(_systemInfoService.SystemInfoDto).ConfigureAwait(false);

		// fetch the user from the database that is equivalent to the user UID who called the request.
		_logger.LogMessage("Getting User");
		var dbUser = await DbContext.Users.SingleAsync(f => f.UID == UserUID).ConfigureAwait(false);

		// Update the last time that the user was logged in to the current time in UTC
		_logger.LogMessage("Updating Last Logged In");
		dbUser.LastLoggedIn = DateTime.UtcNow;

		// Additionally, return to them a welcome message informing them that they are not connected to the server, along with how many users are online.
		_logger.LogMessage("Sending Welcome Message");
		await Clients.Caller.Client_ReceiveServerMessage(MessageSeverity.Information, 
			"Welcome to the Kinkporium's Gagspeak Server, Current Online Users: " + _systemInfoService.SystemInfoDto.OnlineUsers).ConfigureAwait(false);

		// save the changes to the dbUser so they have the updated login time.
		_logger.LogMessage("Saving Changes");
		await DbContext.SaveChangesAsync().ConfigureAwait(false);

		// Finally, compile the ConnectionDto object, and return it to the client who requested it.
		_logger.LogMessage("Returning Connection DTO");
		return new ConnectionDto(new UserData(dbUser.UID, string.IsNullOrWhiteSpace(dbUser.Alias) ? null : dbUser.Alias))
		{
			CurrentClientVersion = _expectedClientVersion,
			ServerVersion = IGagspeakHub.ApiVersion,
		};
	}

	/// <summary> Called by a connected client when they want to check if the client is healthy.
	/// <para> 
	/// This method required the requesting client to have the authorize policy "Authenticated"
	/// It should technically be updating the user on redi's but for now we wont worry about it (unless it is critical to the discord bot)
	/// </para>
	/// </summary>
	[Authorize(Policy = "Authenticated")]
	public async Task<bool> CheckClientHealth()
	{
		_logger.LogMessage("CheckingClientHealth");
		//await UpdateUserOnRedis().ConfigureAwait(false);

		return false;
	}


    /// <summary> Called by a client once they are fully connected to the server.
    /// <para>
    /// This method required the requesting client to have the authorize policy "Authenticated" 
    /// This ensures that only users who have successfully authenticated can establish a real-time connection to the server.
	/// </para>
	/// <para>
	/// _userConnections is the concurrent dictionary of connected users to the server.
    /// </summary>
    [Authorize(Policy = "Authenticated")]
	public override async Task OnConnectedAsync()
	{
		// Attempt to retreive an existing connection ID for the user UID. If it exist it means they are already connected.
		if (_userConnections.TryGetValue(UserUID, out var oldId))
		{
			// if we got here log, a warning that we are updating the users UID to the new connection 
			_logger.LogCallWarning(GagspeakHubLogger.Args(_contextAccessor.GetIpAddress(), "UpdatingId", oldId, Context.ConnectionId));
			// then update the user connections dictionary with the new ID
			_userConnections[UserUID] = Context.ConnectionId;
		}
		// if we reached here, it means that 
		else
		{
			// we have a new connection, so we should log that we are adding a new connection 
			_logger.LogMessage("Adding New Connection");
			// next up, try and log the connection attempt with the user details
			try
			{
				_logger.LogCallInfo(GagspeakHubLogger.Args(_contextAccessor.GetIpAddress(), Context.ConnectionId, UserCharaIdent));
                // then, initialze the player in the online synced pair cache service.
                await _onlineSyncedPairCacheService.InitPlayer(UserUID).ConfigureAwait(false);
                // Update user in Redis, a fast, in-memory data store, for quick access and management of user state (if it is critical to the discord bot)
                // await UpdateUserOnRedis().ConfigureAwait(false);

                // finally, add the user to the user connections dictionary
                _userConnections[UserUID] = Context.ConnectionId;
			}
			catch
			{
				// if at any point we catch an error, then remove the user from the concurrent dictionary of user connections.
				_userConnections.Remove(UserUID, out _);
			}
		}

		// await the base onConnected to finish
		await base.OnConnectedAsync().ConfigureAwait(false);
	}


	/// <summary> Called by a client when they disconnect from the server.
	/// <para>
	/// This method required the requesting client to have the authorize policy "Authenticated" (meaning they have passed the identified policy)
	/// </para>
	/// <para> This method ensure everything is properly disconnected once the function is called upon.</para>
	/// </summary>
	/// <param name="exception">An excption that triggered the disconnected if any.</param>
	[Authorize(Policy = "Authenticated")]
	public override async Task OnDisconnectedAsync(Exception exception)
	{
		// try to get the userUID of the user who just disconnected, and their connection ID to see if it equals the current connection ID.
		if (_userConnections.TryGetValue(UserUID, out var connectionId)
			&& string.Equals(connectionId, Context.ConnectionId, StringComparison.Ordinal))
		{
			// if they were already in the dictionary, log that we have a user disconnecting from the current connection total
			_logger.LogMessage("Removing Connection of 1 user.");

			try
			{
				// dispose the player from the online synced pair cache service
                await _onlineSyncedPairCacheService.DisposePlayer(UserUID).ConfigureAwait(false);

				// log the call info of the user who disconnected
                _logger.LogCallInfo(GagspeakHubLogger.Args(_contextAccessor.GetIpAddress(), Context.ConnectionId, UserCharaIdent));
				// check to see if they disconnected with an exception
				if (exception != null)
				{
					// if they did, we should log a warning letting the server know about it.
					_logger.LogCallWarning(GagspeakHubLogger.Args(_contextAccessor.GetIpAddress(), Context.ConnectionId, exception.Message, exception.StackTrace));
				}

				// remove the users from the redis database (if it is critical to the discord bot)
                // await RemoveUserFromRedis().ConfigureAwait(false);

				// send a function call to all connected pairs of this user that they have gone offline.
                await SendOfflineToAllPairedUsers().ConfigureAwait(false);

				// save the changes to the database (maybe remove this since the line that interacted with the DB is gone here.)
                await DbContext.SaveChangesAsync().ConfigureAwait(false);
            }
			catch { }
			finally
			{
				// finally, remove this user from the concurrent dictionary of connected users.
				_userConnections.Remove(UserUID, out _);
			}
		}
		else
		{
			// if the user was not in the dictionary, log a warning that the user was not in the dictionary.
			_logger.LogCallWarning(GagspeakHubLogger.Args(_contextAccessor.GetIpAddress(), "ObsoleteId", UserUID, Context.ConnectionId));
		}

		// await the base disconnectedAsync method to occur.
		await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
	}
}

