using GagspeakAPI.Dto.VibeRoom;
using GagspeakAPI.Enums;
using GagspeakAPI.Hub;
using GagspeakAPI.Network;
using GagspeakServer.Utils;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Globalization;
using System.Text.Json;

namespace GagspeakServer.Hubs;

/// <summary> handles the hardcore adult immersive content. </summary>
public partial class GagspeakHub
{
    public class VibeRoomParticipantInfo
    {
        public string DisplayName { get; set; } = string.Empty;
        public string[] AllowedUids { get; set; } = Array.Empty<string>();
        public ToyInfo[] Devices { get; set; } = Array.Empty<ToyInfo>();
    }

    public async Task<HubResponse<List<RoomListing>>> SearchForRooms(SearchBase dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        // 1. Get all public room names
        RedisValue[] publicRooms = await _redis.Database.SetMembersAsync(VibeRoomRedis.PublicRoomsKey).ConfigureAwait(false);

        // 2. If tags are specified, get the union of all tag sets and intersect with public rooms
        RedisValue[] filteredRooms = publicRooms;
        if (dto.Tags.Length > 0)
        {
            RedisKey[] tagKeys = dto.Tags.Select(tag => (RedisKey)VibeRoomRedis.TagIndexKey(tag)).ToArray();
            RedisValue[] tagRooms = await _redis.Database.SetCombineAsync(SetOperation.Union, tagKeys).ConfigureAwait(false);
            filteredRooms = publicRooms.Intersect(tagRooms).ToArray();
        }

        // 3. Build RoomListing for each room
        List<RoomListing> listings = new List<RoomListing>();
        foreach (RedisValue roomName in filteredRooms)
        {
            HashEntry[] hash = await _redis.Database.HashGetAllAsync(VibeRoomRedis.RoomHashKey(roomName)).ConfigureAwait(false);
            if (hash.Length == 0) continue;

            Dictionary<RedisValue, RedisValue> dict = hash.ToDictionary(x => x.Name, x => x.Value);

            // Optionally filter by name/description search
            if (!string.IsNullOrEmpty(dto.Input))
            {
                string name = dict["Name"].ToString();
                string desc = dict["Description"].ToString();
                if (!(name.Contains(dto.Input, StringComparison.OrdinalIgnoreCase) ||
                      desc.Contains(dto.Input, StringComparison.OrdinalIgnoreCase)))
                    continue;
            }

            // Get the current participant count
            int participantCount = (int)await _redis.Database.SetLengthAsync(VibeRoomRedis.ParticipantsKey(roomName)).ConfigureAwait(false);

            listings.Add(new RoomListing(dict["Name"], int.Parse(dict["MaxParticipants"], CultureInfo.InvariantCulture))
            {
                CurrentParticipants = participantCount,
                Description = dict["Description"],
                Tags = JsonSerializer.Deserialize<string[]>(dict["Tags"])
            });
        }

        // 4. (Optional) Sort/filter as needed
        if (dto.Order is HubDirection.Descending)
            listings = listings.OrderByDescending(x => x.CurrentParticipants).ToList();
        else
            listings = listings.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();

        return HubResponseBuilder.Yippee(listings);
    }

    /// <summary> Attempts to create a room with the specified room name. </summary>
    public async Task<HubResponse> RoomCreate(RoomCreateRequest dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        string roomName = dto.Name;
        string hashKey = VibeRoomRedis.RoomHashKey(roomName);

        // Check if the room already exists
        if (await _redis.Database.KeyExistsAsync(hashKey).ConfigureAwait(false))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.RoomNameExists);

        // Prepare room metadata
        bool isPublic = string.IsNullOrEmpty(dto.Password);
        string[] tags = dto.Tags ?? Array.Empty<string>();

        // Create the hash entries for the room.
        HashEntry[] hashEntries = new HashEntry[]
        {
            new("Name", roomName),
            new("IsPublic", isPublic ? "1" : "0"),
            new("Password", dto.Password ?? string.Empty),
            new("Description", dto.Description ?? string.Empty),
            new("MaxParticipants", "10"),
            new("HostUid", UserUID),
            new("Tags", JsonSerializer.Serialize(tags)),
            new("CreatedTimeUTC", DateTime.UtcNow.ToString("o"))

        };
        // Assign the hash entries to the Redis database for the room.
        await _redis.Database.HashSetAsync(hashKey, hashEntries).ConfigureAwait(false);

        if (isPublic)
            await _redis.Database.SetAddAsync(VibeRoomRedis.PublicRoomsKey, roomName).ConfigureAwait(false);

        foreach (string tag in tags)
            await _redis.Database.SetAddAsync(VibeRoomRedis.TagIndexKey(tag), roomName).ConfigureAwait(false);

        // Set the room host indexer.
        await _redis.Database.StringSetAsync(VibeRoomRedis.RoomHostKey(roomName), UserUID).ConfigureAwait(false);
        // Add the caller participant to the room.
        await AddParticipantToRoomAsync(roomName, dto.HostData).ConfigureAwait(false);
        // Set this room as the caller's active room.
        await _redis.Database.StringSetAsync(VibeRoomRedis.KinksterRoomKey(UserUID), roomName).ConfigureAwait(false);

        return HubResponseBuilder.Yippee();
    }

    /// <summary> Sends an invite to a user to join a room. </summary>
    public async Task<HubResponse> SendRoomInvite(RoomInvite dto)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(dto));

        if (!await _redis.Database.KeyExistsAsync(VibeRoomRedis.RoomHashKey(dto.RoomName)).ConfigureAwait(false))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.RoomNotFound);

        // It is a cardinal sin to not be the host when sending an invite.
        RedisValue hostUid = await _redis.Database.StringGetAsync(VibeRoomRedis.RoomHostKey(dto.RoomName)).ConfigureAwait(false);
        if (hostUid.IsNullOrEmpty || !hostUid.ToString().Equals(UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotRoomHost);

        // Store the invite as a hash entry under the invitee's invite key
        string inviteKey = VibeRoomRedis.RoomInviteKey(dto.User.UID);

        // Add the invite entry to the inviteKey.
        await _redis.Database.HashSetAsync(inviteKey, [ new(dto.RoomName, dto.AttachedMessage) ]).ConfigureAwait(false);

        // Add the invite to the target's invite list.
        await Clients.User(dto.User.UID).Callback_RoomAddInvite(dto with { User = new(UserUID) }).ConfigureAwait(false);


        return HubResponseBuilder.Yippee();
    }

    /// <summary> Changes the password of an existing room. </summary>
    public async Task<HubResponse> ChangeRoomHost(string roomName, KinksterBase newHost)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(roomName, newHost));

        string roomKey = VibeRoomRedis.RoomHashKey(roomName);

        // Fetch current HostUid
        RedisValue hostUid = await _redis.Database.HashGetAsync(roomKey, "HostUid").ConfigureAwait(false);
        if (hostUid.IsNullOrEmpty)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.RoomNotFound);

        // Only the current host can change the host
        if (!hostUid.ToString().Equals(UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotRoomHost);

        // Update HostUid in the hash and the dedicated key
        await _redis.Database.HashSetAsync(roomKey, [ new("HostUid", newHost.User.UID) ]).ConfigureAwait(false);

        // Update the host UID in the dedicated host key
        await _redis.Database.StringSetAsync(VibeRoomRedis.RoomHostKey(roomName), newHost.User.UID).ConfigureAwait(false);

        // Notify all participants in the room
        RedisValue[] participantUids = await _redis.Database.SetMembersAsync(VibeRoomRedis.ParticipantsKey(roomName)).ConfigureAwait(false);
        await Clients.Users(participantUids.ToStringArray()).Callback_RoomHostChanged(newHost.User).ConfigureAwait(false);

        return HubResponseBuilder.Yippee();
    }

    /// <summary> Changes the password of an existing room. </summary>
    public async Task<HubResponse> ChangeRoomPassword(string roomName, string newPass)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(roomName, newPass));

        string roomKey = VibeRoomRedis.RoomHashKey(roomName);

        RedisValue[] fields = await _redis.Database.HashGetAsync(roomKey, [ "HostUid", "Password" ]).ConfigureAwait(false);
        if (fields.Length != 2 || fields[0].IsNullOrEmpty)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.RoomNotFound);

        string hostUid = fields[0].ToString();
        string oldPassword = fields[1].ToString() ?? string.Empty;

        // Only the host can change the password.
        if (!hostUid.Equals(UserUID, StringComparison.Ordinal))
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotRoomHost);

        // Determine new IsPublic value
        bool wasPublic = string.IsNullOrEmpty(oldPassword);
        bool isNowPublic = string.IsNullOrEmpty(newPass);

        // Update password and IsPublic in the hash
        HashEntry[] hashUpdates = new HashEntry[]
        {
            new("Password", newPass ?? string.Empty),
            new("IsPublic", isNowPublic ? "1" : "0")
        };
        await _redis.Database.HashSetAsync(roomKey, hashUpdates).ConfigureAwait(false);

        // Update public room set if public status changed
        if (wasPublic && !isNowPublic)
            await _redis.Database.SetRemoveAsync(VibeRoomRedis.PublicRoomsKey, roomName).ConfigureAwait(false);
        else if (!wasPublic && isNowPublic)
            await _redis.Database.SetAddAsync(VibeRoomRedis.PublicRoomsKey, roomName).ConfigureAwait(false);

        return HubResponseBuilder.Yippee();
    }

    /// <summary> Allows a user to join the room. </summary>
    public async Task<HubResponse<List<RoomParticipant>>> RoomJoin(string name, string pass, RoomParticipant joiningUser)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(name, pass, joiningUser));

        string roomHashKey = VibeRoomRedis.RoomHashKey(name);
        // Ensure the room exists and fetch required fields
        RedisValue[] fields = await _redis.Database.HashGetAsync(roomHashKey, [ "IsPublic", "Password", "MaxParticipants" ]).ConfigureAwait(false);
        if (fields.Length != 3 || fields[0].IsNullOrEmpty)
            return HubResponseBuilder.AwDangIt<List<RoomParticipant>>(GagSpeakApiEc.RoomNotFound);

        // the hashed values.
        bool isPublic = fields[0] == "1";
        string roomPassword = fields[1].ToString() ?? string.Empty;
        int maxParticipants = int.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int mp) ? mp : 10;

        // If not public, check the password
        if (!isPublic && !roomPassword.Equals(pass))
            return HubResponseBuilder.AwDangIt<List<RoomParticipant>>(GagSpeakApiEc.InvalidPassword);

        // Check participant count
        string participantsKey = VibeRoomRedis.ParticipantsKey(name);
        int currentCount = (int)await _redis.Database.SetLengthAsync(participantsKey).ConfigureAwait(false);
        if (currentCount >= maxParticipants)
            return HubResponseBuilder.AwDangIt<List<RoomParticipant>>(GagSpeakApiEc.RoomIsFull);

        // Fetch other participants
        string[] otherUserUids = (await _redis.Database.SetMembersAsync(participantsKey).ConfigureAwait(false)).ToStringArray();
        List<RoomParticipant> otherParticipants = new List<RoomParticipant>();
        foreach (string uid in otherUserUids)
        {
            string partKey = VibeRoomRedis.ParticipantDataKey(name, uid);
            RedisValue json = await _redis.Database.StringGetAsync(partKey).ConfigureAwait(false);
            if (json.IsNullOrEmpty) continue;

            VibeRoomParticipantInfo participantInfo = JsonSerializer.Deserialize<VibeRoomParticipantInfo>(json);
            if (participantInfo is null)
                continue;

            otherParticipants.Add(new RoomParticipant(new(uid), participantInfo.DisplayName)
            {
                AllowedUids = participantInfo.AllowedUids.ToList(),
                Devices = participantInfo.Devices.ToList()
            });
        }

        // Add the Caller to the room.
        await AddParticipantToRoomAsync(name, joiningUser).ConfigureAwait(false);
        // inform all other UID's in the room that the caller just joined.
        await Clients.Users(otherUserUids).Callback_RoomJoin(joiningUser).ConfigureAwait(false);

        // Update the user's current vibe room.
        await _redis.Database.StringSetAsync(VibeRoomRedis.KinksterRoomKey(UserUID), name).ConfigureAwait(false);
        return HubResponseBuilder.Yippee(otherParticipants);
    }

    /// <summary> When the Caller wishes to leave their current room. </summary>
    /// <remarks> DEVNOTE: Please do a stability check to make sure all tag associations are removed to avoid DB bloat overtime. </remarks>
    public async Task<HubResponse> RoomLeave()
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(UserUID));

        // Get the room the user is currently in
        string userRoomKey = VibeRoomRedis.KinksterRoomKey(UserUID);
        RedisValue roomName = await _redis.Database.StringGetAsync(userRoomKey).ConfigureAwait(false);
        if (!roomName.HasValue)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotInRoom);

        // get the list of participants for the room we are leaving.
        string participantsKey = VibeRoomRedis.ParticipantsKey(roomName);
        string participantDataKey = VibeRoomRedis.ParticipantDataKey(roomName, UserUID);

        // Remove UID from the participant set (the set contains only UIDs)
        await _redis.Database.SetRemoveAsync(participantsKey, UserUID).ConfigureAwait(false);
        // Delete this user's participant data
        await _redis.Database.KeyDeleteAsync(participantDataKey).ConfigureAwait(false);
        // Delete the user's room tracker key (remove them from the room)
        await _redis.Database.KeyDeleteAsync(userRoomKey).ConfigureAwait(false);

        // Check remaining participants
        RedisValue[] remainingUids = await _redis.Database.SetMembersAsync(participantsKey).ConfigureAwait(false);
        string[] uidList = remainingUids.ToStringArray();

        if (uidList.Length == 0)
        {
            // If none remain, clean up the room entirely
            await _redis.Database.KeyDeleteAsync(VibeRoomRedis.RoomHashKey(roomName)).ConfigureAwait(false);
            await _redis.Database.KeyDeleteAsync(participantsKey).ConfigureAwait(false);
            await _redis.Database.KeyDeleteAsync(VibeRoomRedis.RoomHostKey(roomName)).ConfigureAwait(false);
            // Remove from public rooms if applicable
            await _redis.Database.KeyDeleteAsync($"{VibeRoomRedis.PublicRoomsKey}:{roomName}").ConfigureAwait(false);
            // remove the room's keys where they exist.
            RedisValue tagsValue = await _redis.Database.HashGetAsync(VibeRoomRedis.RoomHashKey(roomName), "Tags").ConfigureAwait(false);
            if (!tagsValue.IsNullOrEmpty && JsonSerializer.Deserialize<string[]>(tagsValue) is { } tags)
                foreach (string tag in tags)
                    await _redis.Database.SetRemoveAsync(VibeRoomRedis.TagIndexKey(tag), roomName).ConfigureAwait(false);
        }
        else
        {
            await Clients.Users(uidList).Callback_RoomLeave(new(UserUID)).ConfigureAwait(false);
        }

        return HubResponseBuilder.Yippee();
    }

    /// <summary> Grants access to a user in the room. </summary>
    public async Task<HubResponse> RoomGrantAccess(KinksterBase allowedUser)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(allowedUser));

        // get the room they are in, in the first place.
        string userRoomKey = VibeRoomRedis.KinksterRoomKey(UserUID);
        // aquire that room name.
        RedisValue roomName = await _redis.Database.StringGetAsync(userRoomKey).ConfigureAwait(false);
        if (!roomName.HasValue)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotInRoom);

        // get our participant data key and add them to the list of allowed users.
        string participantDataKey = VibeRoomRedis.ParticipantDataKey(roomName, UserUID);
        RedisValue participantJson = await _redis.Database.StringGetAsync(participantDataKey).ConfigureAwait(false);
        if (participantJson.IsNullOrEmpty)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.RoomParticipantNotFound);

        // deserialize the participant data.
        RoomParticipant participant = JsonSerializer.Deserialize<RoomParticipant>(participantJson);
        if (participant == null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // Add the user.
        participant.AllowedUids.Add(allowedUser.User.UID);

        // Serialize the updated participant data.
        string updatedParticipantJson = JsonSerializer.Serialize(participant);
        // Save the updated participant data back to Redis.
        await _redis.Database.StringSetAsync(participantDataKey, updatedParticipantJson).ConfigureAwait(false);

        // Notify the user that they have been granted access.
        await Clients.User(allowedUser.User.UID).Callback_RoomAccessGranted(new(UserUID)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    /// <summary> Revokes access from a user in the room. </summary>
    public async Task<HubResponse> RoomRevokeAccess(KinksterBase restrictedUser)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(restrictedUser));

        // get the room they are in, in the first place.
        string userRoomKey = VibeRoomRedis.KinksterRoomKey(UserUID);
        // aquire that room name.
        RedisValue roomName = await _redis.Database.StringGetAsync(userRoomKey).ConfigureAwait(false);
        if (!roomName.HasValue)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotInRoom);

        // get our participant data key and remove them from the list of allowed users.
        string participantDataKey = VibeRoomRedis.ParticipantDataKey(roomName, UserUID);
        RedisValue participantJson = await _redis.Database.StringGetAsync(participantDataKey).ConfigureAwait(false);
        if (participantJson.IsNullOrEmpty)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.RoomParticipantNotFound);

        // deserialize the participant data.
        RoomParticipant participant = JsonSerializer.Deserialize<RoomParticipant>(participantJson);
        if (participant == null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // Remove the user.
        participant.AllowedUids.Remove(restrictedUser.User.UID);
        // Serialize the updated participant data.
        string updatedParticipantJson = JsonSerializer.Serialize(participant);
        // Save the updated participant data back to Redis.
        await _redis.Database.StringSetAsync(participantDataKey, updatedParticipantJson).ConfigureAwait(false);

        // Notify the user that they have been revoked access.
        await Clients.User(restrictedUser.User.UID).Callback_RoomAccessRevoked(new(UserUID)).ConfigureAwait(false);
        return HubResponseBuilder.Yippee();
    }

    /// <summary> Pushes device update (e.g., for battery level, motor settings) to the room. </summary>
    public async Task<HubResponse> RoomPushDeviceUpdate(ToyInfo deviceInfo)
    {
        _logger.LogCallInfo(GagspeakHubLogger.Args(deviceInfo));

        // get the room they are in, in the first place.
        string userRoomKey = VibeRoomRedis.KinksterRoomKey(UserUID);
        // aquire that room name.
        RedisValue roomName = await _redis.Database.StringGetAsync(userRoomKey).ConfigureAwait(false);
        if (!roomName.HasValue)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotInRoom);

        // get our participant data key.
        string participantDataKey = VibeRoomRedis.ParticipantDataKey(roomName, UserUID);
        RedisValue participantJson = await _redis.Database.StringGetAsync(participantDataKey).ConfigureAwait(false);
        if (participantJson.IsNullOrEmpty)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.RoomParticipantNotFound);

        // deserialize the participant data.
        RoomParticipant participant = JsonSerializer.Deserialize<RoomParticipant>(participantJson);
        if (participant == null)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NullData);

        // If the device does not exist, add it, otherwise if it does update it's InteractableState.
        if (participant.Devices.FirstOrDefault(d => d.BrandName == deviceInfo.BrandName) is { } match)
            match = deviceInfo;
        else
            participant.Devices.Add(deviceInfo);

        // Serialize the updated participant data.
        string updatedParticipantJson = JsonSerializer.Serialize(participant);
        // Save the updated participant data back to Redis.
        await _redis.Database.StringSetAsync(participantDataKey, updatedParticipantJson).ConfigureAwait(false);
        // Notify all participants in the room about the device update.
        RedisValue[] participantUids = await _redis.Database.SetMembersAsync(VibeRoomRedis.ParticipantsKey(roomName)).ConfigureAwait(false);

        List<string> uidList = participantUids.Select(x => (string)x!).ToList();
        await Clients.Users(uidList).Callback_RoomDeviceUpdate(new(UserUID), deviceInfo).ConfigureAwait(false);
        return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotYetImplemented);
    }

    /// <summary> Sends a data stream (vibration/rotation data) to users in the room. </summary>
    public async Task<HubResponse> RoomSendDataStream(ToyDataStream streamDto)
    {
        _logger.LogMessage("User sent dataStream!");

        RedisValue roomName = await _redis.Database.StringGetAsync(VibeRoomRedis.KinksterRoomKey(UserUID)).ConfigureAwait(false);
        if (!roomName.HasValue)
            return HubResponseBuilder.AwDangIt(GagSpeakApiEc.NotInRoom);

        // get the other uid's in the room.
        string[] participantUids = (await _redis.Database.SetMembersAsync(VibeRoomRedis.ParticipantsKey(roomName)).ConfigureAwait(false)).ToStringArray();

        // Send our the datastream to all intended participants.
        foreach (UserDeviceStream dataStreamChunk in streamDto.DataStream)
        {
            // If the participant is allowed to receive this data, send it.
            if (participantUids.Contains(dataStreamChunk.User.UID, StringComparer.OrdinalIgnoreCase))
                await Clients.User(dataStreamChunk.User.UID).Callback_RoomIncDataStream(new(dataStreamChunk.Devices, streamDto.Timestamp)).ConfigureAwait(false);
        }

        return HubResponseBuilder.Yippee();
    }
    
    // Helper method to add a participant to a room.
    private async Task AddParticipantToRoomAsync(string roomName, RoomParticipant participant)
    {
        string participantsKey = VibeRoomRedis.ParticipantsKey(roomName);
        string participantDataKey = VibeRoomRedis.ParticipantDataKey(roomName, participant.User.UID);

        // Add user id to participants set
        await _redis.Database.SetAddAsync(participantsKey, participant.User.UID).ConfigureAwait(false);

        // Save participant data serialized as JSON
        string participantJson = JsonSerializer.Serialize(new VibeRoomParticipantInfo
        {
            DisplayName = participant.DisplayName,
            AllowedUids = participant.AllowedUids.ToArray(),
            Devices = participant.Devices.ToArray()
        });
        // Store participant data in Redis
        await _redis.Database.StringSetAsync(participantDataKey, participantJson).ConfigureAwait(false);
    }
}