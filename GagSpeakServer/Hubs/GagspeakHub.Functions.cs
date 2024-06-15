using GagspeakServer.Models;
using Microsoft.EntityFrameworkCore;
using GagspeakServer.Utils;
using Microsoft.IdentityModel.Tokens;
using Gagspeak.API.Data;
using Microsoft.AspNetCore.SignalR;
using GagspeakServer.Data;

namespace GagspeakServer.Hubs;

/// <summary>
/// This partial class of the GagspeakHub handles helper functions used in the servers function responses to make them cleaner.
/// <para> AKA, This is the "Messy code file" </para>
/// <para> Also contains the Client Caller context UserCharaIdentity, the UserUID, and the Continent</para>
/// </summary>
public partial class GagspeakHub
{
    public string UserCharaIdent => Context.User?.Claims?.SingleOrDefault(c => string.Equals(c.Type, GagspeakClaimTypes.CharaIdent, StringComparison.Ordinal))?.Value ?? throw new Exception("No Chara Ident in Claims");

    public string UserUID => Context.User?.Claims?.SingleOrDefault(c => string.Equals(c.Type, GagspeakClaimTypes.Uid, StringComparison.Ordinal))?.Value ?? throw new Exception("No UID in Claims");

    public string Continent => Context.User?.Claims?.SingleOrDefault(c => string.Equals(c.Type, GagspeakClaimTypes.Continent, StringComparison.Ordinal))?.Value ?? "UNK";

    /// <summary> Helper function to remove a assist with properly deleting a user from all locations in where it was stored.</summary>
    /// <param name="user">the user to delete</param>
    private async Task DeleteUser(User user)
    {
        // fetch the users own pair data list from the database
        List<ClientPair> ownPairData = await DbContext.ClientPairs.Where(u => u.User.UID == user.UID).ToListAsync().ConfigureAwait(false);
        // fetch the users authentication row from the database
        Auth auth = await DbContext.Auth.SingleAsync(u => u.UserUID == user.UID).ConfigureAwait(false);
        // fetch the user's profile data from the database
        UserProfileData? userProfileData = await DbContext.UserProfileData.SingleOrDefaultAsync(u => u.UserUID == user.UID).ConfigureAwait(false);

        // if the user's profile data had content / was in in the database, remove it
        if (userProfileData != null)
        {
            DbContext.Remove(userProfileData);
        }
        
        // remove the user's client pair data from the client pairs table. (relative to them being the left side of the keys in the combined key pair)
        DbContext.ClientPairs.RemoveRange(ownPairData);
        
        // fetch a list of the client pairs who had their own connection to this user who is now deleting their account. 
        List<ClientPair> otherPairData = await DbContext.ClientPairs.Include(u => u.User)
            .Where(u => u.OtherUser.UID == user.UID).AsNoTracking().ToListAsync().ConfigureAwait(false);
        
        // for each of those clients in the database with a pairing to the user removing their account,
        foreach (var pair in otherPairData)
        {
            // invoke a method on their client telling them to remove that user from their client pair list.
            await Clients.User(pair.UserUID).Client_UserRemoveClientPair(new(user.ToUserData())).ConfigureAwait(false);
        }
        
        // remove the range from the client pairs of the otherPairData
        DbContext.ClientPairs.RemoveRange(otherPairData);
        // remove the user requesting deletion from the user table
        DbContext.Users.Remove(user);
        // remove the users authentication from the auth table.
        DbContext.Auth.Remove(auth);
        // save the changes to the database.
        await DbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary> Simpler helper function that gets all paired users that are present in the client callers pair list.</summary>
    /// <param name="uid">UID to search for paired users of</param>
    /// <returns>a list of UID's of all users that are paired with the provided UID</returns>
    private async Task<List<string>> GetAllPairedUnpausedUsers(string? uid = null)
    {
        // if no UID is provided, set it to the client caller context UserUID
        uid ??= UserUID;
        // return the list of UID's of all users that are paired with the provided UID
        return (await GetSyncedUnpausedOnlinePairs(UserUID).ConfigureAwait(false));
    }

    /// <summary> Helper to get the total number of users who are online currently from the list of passed in UID's.</summary>
    private async Task<Dictionary<string, string>> GetOnlineUsers(List<string> uids)
    {
        var result = await _redis.GetAllAsync<string>(uids.Select(u => "UID:" + u).ToHashSet(StringComparer.Ordinal)).ConfigureAwait(false);
        return uids.Where(u => result.TryGetValue("UID:" + u, out var ident) && !string.IsNullOrEmpty(ident)).ToDictionary(u => u, u => result["UID:" + u], StringComparer.Ordinal);
    }

    /// <summary> Helper function to get the user's identity from the redis by their UID</summary>
    private async Task<string> GetUserIdent(string uid)
    {
        if (uid.IsNullOrEmpty()) return string.Empty;
        return await _redis.GetAsync<string>("UID:" + uid).ConfigureAwait(false);
    }

    /// <summary> Helper function to remove a user from the redis by their UID</summary>
    private async Task RemoveUserFromRedis()
    {
        await _redis.RemoveAsync("UID:" + UserUID, StackExchange.Redis.CommandFlags.FireAndForget).ConfigureAwait(false);
    }

    /// <summary> A helper function to update the user's identity on the redi's by their UID</summary>
    private async Task UpdateUserOnRedis()
    {
        await _redis.AddAsync("UID:" + UserUID, UserCharaIdent, TimeSpan.FromSeconds(60), StackExchange.Redis.When.Always, StackExchange.Redis.CommandFlags.FireAndForget).ConfigureAwait(false);
    }

    /// <summary> Helper function that will collect the user pairs of a client caller, 
    /// and send to all the client pairs a invoke function that the client caller has gone offline. </summary>
    private async Task<List<string>> SendOfflineToAllPairedUsers()
    {
        // the list of users to send the message to
        var usersToSendDataTo = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
        // our user
        var self = await DbContext.Users.AsNoTracking().SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);
        // send to all our clientpairs that the client caller went offline.
        await Clients.Users(usersToSendDataTo).Client_UserSendOffline(new(self.ToUserData())).ConfigureAwait(false);
        // return the list of users to send the message to
        return usersToSendDataTo;
    }

    /// <summary> Helper function that will collect the user pairs of a client caller,
    /// and send to all the client pairs a invoke function that the client caller has gone online. </summary>
    private async Task<List<string>> SendOnlineToAllPairedUsers()
    {
        // the same as offline function above, but for online really
        var usersToSendDataTo = await GetAllPairedUnpausedUsers().ConfigureAwait(false);
        var self = await DbContext.Users.AsNoTracking().SingleAsync(u => u.UID == UserUID).ConfigureAwait(false);
        await Clients.Users(usersToSendDataTo).Client_UserSendOnline(new(self.ToUserData(), UserCharaIdent)).ConfigureAwait(false);
        // return the list of users to send the message to
        return usersToSendDataTo;
    }


    /// <summary> A helper function to get the pair information of a user and another user from the database</summary>
    private async Task<UserInfo?> GetPairInfo(string uid, string otheruid)
    {
        var clientPairs = from cp in DbContext.ClientPairs.AsNoTracking().Where(u => u.UserUID == uid && u.OtherUserUID == otheruid)
                          join cp2 in DbContext.ClientPairs.AsNoTracking().Where(u => u.OtherUserUID == uid && u.UserUID == otheruid)
                          on new
                          {
                              UserUID = cp.UserUID, // Create an anonymous object with UserUID
                              OtherUserUID = cp.OtherUserUID // and OtherUserUID
                          }
                          equals new
                          {
                              UserUID = cp2.OtherUserUID, // Join where the UserUID matches OtherUserUID of cp2
                              OtherUserUID = cp2.UserUID // and OtherUserUID matches UserUID of cp2
                          } into joined // Perform a left join
                          from c in joined.DefaultIfEmpty() // Include results even if there are no matching elements
                          where cp.UserUID == uid // Ensure we are only working with the given uid
                          select new
                          {
                              UserUID = cp.UserUID, // Select UserUID
                              OtherUserUID = cp.OtherUserUID, // Select OtherUserUID
                              Synced = c != null // Check if the join found a match, indicating synchronization
                          };

        var result = from user in clientPairs // Start from the clientPairs result
                     join u in DbContext.Users.AsNoTracking() on user.OtherUserUID equals u.UID // Join with Users table where OtherUserUID matches UID
                     where user.UserUID == uid && u.UID == user.OtherUserUID // Ensure UserUID and OtherUserUID match the given uid
                     select new
                     {
                         UserUID = user.UserUID, // Select UserUID
                         OtherUserUID = user.OtherUserUID, // Select OtherUserUID
                         OtherUserAlias = u.Alias, // Select OtherUserAlias from Users table
                         Synced = user.Synced // Select Synced status from clientPairs
                     };

        var resultList = await result.AsNoTracking().ToListAsync().ConfigureAwait(false); // Execute the query asynchronously and get the result list

        if (!resultList.Any()) return null; // If there are no results, return null

        return new UserInfo(resultList[0].OtherUserAlias, // Create UserInfo with the alias of the first user in the result list
            resultList.SingleOrDefault(p => true)?.Synced ?? false, // Set Synced status based on the presence in the result list
            resultList.Max(p => p.Synced), // Get the maximum Synced value from the result list
            new List<string> { "//GAGSPEAK//DIRECT" }); // Provide a fixed list indicating direct communication
    }


    /// <summary> A helper function to get the pair information of a user and another user from the database</summary>
    private async Task<Dictionary<string, UserInfo>> GetAllPairInfo(string uid)
    {
        var clientPairs = from cp in DbContext.ClientPairs.AsNoTracking().Where(u => u.UserUID == uid) // Get all ClientPairs where the UserUID matches the given uid
                          join cp2 in DbContext.ClientPairs.AsNoTracking().Where(u => u.OtherUserUID == uid) // Join with ClientPairs where OtherUserUID matches the given uid
                          on new
                          {
                              UserUID = cp.UserUID, // Create an anonymous object with UserUID
                              OtherUserUID = cp.OtherUserUID // and OtherUserUID
                          }
                          equals new
                          {
                              UserUID = cp2.OtherUserUID, // Join where the UserUID matches OtherUserUID of cp2
                              OtherUserUID = cp2.UserUID // and OtherUserUID matches UserUID of cp2
                          } into joined // Perform a left join
                          from c in joined.DefaultIfEmpty() // Include results even if there are no matching elements
                          where cp.UserUID == uid // Ensure we are only working with the given uid
                          select new
                          {
                              UserUID = cp.UserUID, // Select UserUID
                              OtherUserUID = cp.OtherUserUID, // Select OtherUserUID
                              Synced = c != null // Check if the join found a match, indicating synchronization
                          };

        var result = from user in clientPairs // Start from the clientPairs result
                     join u in DbContext.Users.AsNoTracking() on user.OtherUserUID equals u.UID // Join with Users table where OtherUserUID matches UID
                     where user.UserUID == uid && u.UID == user.OtherUserUID // Ensure UserUID and OtherUserUID match the given uid
                     select new
                     {
                         UserUID = user.UserUID, // Select UserUID
                         OtherUserUID = user.OtherUserUID, // Select OtherUserUID
                         OtherUserAlias = u.Alias, // Select OtherUserAlias from Users table
                         Synced = user.Synced // Select Synced status from clientPairs (bidirectional pairing)
                     };

        var resultList = await result.AsNoTracking().ToListAsync().ConfigureAwait(false); // Execute the query asynchronously and get the result list
        return resultList.GroupBy(g => g.OtherUserUID, StringComparer.Ordinal).ToDictionary(g => g.Key, g => // Group results by OtherUserUID and convert to dictionary
        {
            return new UserInfo(g.First().OtherUserAlias, // Create UserInfo with the alias of the first user in the group
                g.SingleOrDefault(p => true)?.Synced ?? false, // Set Synced status based on the presence in the group
                g.Max(p => p.Synced), // Get the maximum Synced value from the group
                new List<string> { "//GAGSPEAK//DIRECT" }); // Provide a fixed list indicating direct communication
        }, StringComparer.Ordinal);
    }

    /// <summary> A helper function to get the pair information of a user and another user from the database</summary>
    private async Task<List<string>> GetSyncedUnpausedOnlinePairs(string uid)
    {
        var clientPairs = from cp in DbContext.ClientPairs.AsNoTracking().Where(u => u.UserUID == uid) // Get all ClientPairs where the UserUID matches the given uid
                          join cp2 in DbContext.ClientPairs.AsNoTracking().Where(u => u.OtherUserUID == uid) // Join with ClientPairs where OtherUserUID matches the given uid
                          on new
                          {
                              UserUID = cp.UserUID, // Create an anonymous object with UserUID
                              OtherUserUID = cp.OtherUserUID // and OtherUserUID
                          }
                          equals new
                          {
                              UserUID = cp2.OtherUserUID, // Join where the UserUID matches OtherUserUID of cp2
                              OtherUserUID = cp2.UserUID // and OtherUserUID matches UserUID of cp2
                          } into joined // Perform a left join
                          from c in joined.DefaultIfEmpty() // Include results even if there are no matching elements
                          where cp.UserUID == uid && c.UserUID != null // Ensure we are only working with the given uid and that there is a reciprocal match
                          select new
                          {
                              UserUID = cp.UserUID, // Select UserUID
                              OtherUserUID = cp.OtherUserUID // Select OtherUserUID
                          };

        var result = from user in clientPairs // Start from the clientPairs result
                     where user.UserUID == uid // Ensure UserUID matches the given uid
                     select user.OtherUserUID; // Select OtherUserUID

        return await result.Distinct().AsNoTracking().ToListAsync().ConfigureAwait(false); // Execute the query asynchronously, get distinct results, and convert to a list
    }


    public record UserInfo(string Alias, bool IndividuallyPaired, bool IsSynced, List<string> GIDs);
}