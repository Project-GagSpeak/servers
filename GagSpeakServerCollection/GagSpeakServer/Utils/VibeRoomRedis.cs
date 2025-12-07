using GagspeakAPI.Network;

namespace GagspeakServer.Hubs;

// ------------------- Redis Key Cheatsheet for VibeRooms -------------------
public static class VibeRoomRedis
{
    /// <summary> Hashed Metadata for the VibeRoom Info. (fields: Name, IsPublic, Password, Description, MaxParticipants, HostUid, Tags). </summary>
    /// <remarks> Redis hash key: <c>$"VibeRoom:Room:{roomName}"</c> </remarks>
    public static string RoomHashKey(string roomName) => $"VibeRoom:Room:{roomName}";

    /// <summary>
    ///     Set of all public room names for fast public room listing.
    /// </summary>
    public const string PublicRoomsKey = "VibeRoom:PublicRooms";

    /// <summary>
    ///     Set of room names for each tag, for fast tag-based searching.
    /// </summary>
    /// <remarks> Redis set key: <c>$"VibeRoom:Tag:{tag}"</c> </remarks>
    public static string TagIndexKey(string tag) => $"VibeRoom:Tag:{tag}";

    /// <summary>
    ///     Set of kinkster UID's in a VibeRoom.
    /// </summary>
    /// <remarks> Redis set key: <c>$"VibeRoom:Participants:{roomName}"</c> </remarks>
    public static string ParticipantsKey(string roomName) => $"VibeRoom:Participants:{roomName}";

    /// <summary>
    ///     Contains the serialized <see cref="RoomParticipant"/> data for a <paramref name="userUid"/> 
    ///     in a <paramref name="roomName"/>, which contains: <para/>
    ///     - DisplayName (the display name of the participant) <para/>
    ///     - AllowedParticipantUid's (the list of UIDs allowed to use <paramref name="userUid"/>'s toys) <para/>
    ///     - Devices (the list of devices <paramref name="userUid"/> has setup.) <para/>
    /// </summary>
    /// <remarks> Raw Redi's string value is: <c>$"VibeRoom:ParticipantData:{roomName}:{userUid}"</c></remarks>
    public static string ParticipantDataKey(string roomName, string userUid) => $"VibeRoom:ParticipantData:{roomName}:{userUid}";

    /// <summary>
    ///     Tracks which VibeRoom a user is currently in.
    /// </summary>
    /// <remarks> Redis string key: <c>$"VibeRoom:KinksterRoom:{userUid}"</c> </remarks>
    public static string KinksterRoomKey(string userUid) => $"VibeRoom:KinksterRoom:{userUid}";

    /// <summary>
    ///     Stores the host UID for a room (optional, if you want a dedicated key).
    /// </summary>
    /// <remarks> Redis string key: <c>$"VibeRoom:Host:{roomName}"</c> </remarks>
    public static string RoomHostKey(string roomName) => $"VibeRoom:Host:{roomName}";

    /// <summary>
    ///     Stores invites for a user to join a room.
    /// </summary>
    /// <remarks> Redis string key: <c>$"VibeRoom:Invites:{targetUid}"</c> </remarks>
    public static string RoomInviteKey(string targetUid) => $"VibeRoom:Invites:{targetUid}";
}