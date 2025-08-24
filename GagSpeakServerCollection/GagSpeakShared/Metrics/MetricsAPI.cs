namespace GagspeakShared.Metrics;

/// <summary> 
///     *insert some funny joke about mega corps taking all your data here*
/// </summary>
public class MetricsAPI
{
    // info about connection.
    public const string CounterInitializedConnections = "gagspeak_initialized_connections";
    public const string GaugeConnections = "gagspeak_connections";
    public const string GaugeAuthorizedConnections = "gagspeak_authorized_connections";
    public const string GaugeAvailableWorkerThreads = "gagspeak_available_threadpool";
    public const string GaugeAvailableIOWorkerThreads = "gagspeak_available_threadpool_io";

    // Info about users.
    public const string CounterAuthenticationRequests = "gagspeak_authentication_requests";
    public const string CounterAuthenticationSuccess = "gagspeak_authentication_success";
    public const string CounterAuthenticationFailed = "gagspeak_authentication_failed";
    public const string GaugeUsersRegistered = "gagspeak_users_registered";
    public const string CounterUsersRegisteredDeleted = "gagspeak_users_registered_deleted";
    public const string GaugePairs = "gagspeak_pairs";

    // ShareHub data.
    public const string GaugeShareHubPatterns = "gagspeak_sharehub_patterns";
    public const string GaugeShareHubMoodles = "gagspeak_sharehub_moodles";
    public const string CounterUploadedPatterns = "gagspeak_uploaded_patterns";
    public const string CounterUploadedMoodles = "gagspeak_uploaded_moodles";
    public const string CounterPatternDownloads = "gagspeak_pattern_downloads";
    public const string GaugePatternLikes = "gagspeak_pattern_likes";
    public const string GaugeMoodleLikes = "gagspeak_moodle_likes";
    public const string CounterShareHubSearches = "gagspeak_sharehub_searches";

    // Chat & KinkPlates™.
    public const string CounterGlobalChatMessages = "gagspeak_global_chat_messages";
    public const string CounterKinkPlateUpdates = "gagspeak_kinkplate_updates";
    public const string CounterKinkPlateReportsCreated = "gagspeak_kinkplate_reports_created";

    // IPC
    public const string CounterSentAppearanceFull = "gagspeak_sent_appearance_full";
    public const string CounterSentAppearanceLight = "gagspeak_sent_appearance_light";
    public const string CounterSentAppearanceSingle = "gagspeak_sent_appearance_glamour";
    public const string CounterMoodleTransferFull = "gagspeak_moodle_transfer_full";
    public const string CounterMoodleTransferSM = "gagspeak_moodle_transfer_sm";
    public const string CounterMoodleTransferStatus = "gagspeak_moodle_transfer_status";
    public const string CounterMoodleTransferPreset = "gagspeak_moodle_transfer_preset";
    public const string CounterMoodlesAppliedId = "gagspeak_moodles_applied_id";
    public const string CounterMoodlesAppliedStatus = "gagspeak_moodles_applied_status";
    public const string CounterMoodlesRemoved = "gagspeak_moodles_removed";
    public const string CounterMoodlesCleared = "gagspeak_moodles_cleared";

    // Active States
    public const string CounterStateTransferFull = "gagspeak_statetransfers_full";
    public const string CounterStateTransferGags = "gagspeak_statetransfers_gags";
    public const string CounterStateTransferRestrictions = "gagspeak_statetransfers_restrictions";
    public const string CounterStateTransferRestraint = "gagspeak_statetransfers_restraint";
    public const string CounterStateTransferCollar = "gagspeak_statetransfers_collar";
    public const string CounterStateTransferLoot = "gagspeak_statetransfers_loot";
    public const string CounterStateTransferToys = "gagspeak_statetransfers_toys";
    public const string CounterStateTransferPattern = "gagspeak_statetransfers_pattern";
    public const string CounterStateTransferAlarms = "gagspeak_statetransfers_alarms";
    public const string CounterStateTransferTriggers = "gagspeak_statetransfers_triggers";

    // Data Updates
    public const string CounterDataUpdateGags = "gagspeak_dataupdate_gags";
    public const string CounterDataUpdateRestrictions = "gagspeak_dataupdate_restrictions";
    public const string CounterDataUpdateRestraint = "gagspeak_dataupdate_restraint";
    public const string CounterDataUpdateCollar = "gagspeak_dataupdate_collar";
    public const string CounterDataUpdateLoot = "gagspeak_dataupdate_loot";
    public const string CounterDataUpdatePattern = "gagspeak_dataupdate_pattern";
    public const string CounterDataUpdateAlarms = "gagspeak_dataupdate_alarms";
    public const string CounterDataUpdateTriggers = "gagspeak_dataupdate_triggers";
    public const string CounterDataUpdateAllowances = "gagspeak_dataupdate_active_allowances";

    // Requests
    public const string GaugePendingKinksterRequests = "gagspeak_pending_kinkster_requests";
    public const string GaugePendingCollarRequests = "gagspeak_pending_collar_requests";
    public const string CounterKinksterRequestsCreated = "gagspeak_kinkster_requests_created";
    public const string CounterKinksterRequestsAccepted = "gagspeak_kinkster_requests_accepted";
    public const string CounterKinksterRequestsRejected = "gagspeak_kinkster_requests_rejected";
    public const string CounterCollarRequestsCreated = "gagspeak_collar_requests_created";
    public const string CounterCollarRequestsAccepted = "gagspeak_collar_requests_accepted";
    public const string CounterCollarRequestsRejected = "gagspeak_collar_requests_rejected";

    // Permissions
    public const string CounterPermissionChangeGlobal = "gagspeak_permission_change_global";
    public const string CounterPermissionChangeHardcore = "gagspeak_permission_change_hardcore";
    public const string CounterPermissionChangeUnique = "gagspeak_permission_change_unique";
    public const string CounterPermissionChangeAccess = "gagspeak_permission_change_access";

    // Misc
    public const string CounterSafewordUsed = "gagspeak_safeword_used";
    public const string CounterNamesSent = "gagspeak_names_sent";
    public const string CounterHypnoticEffectsSent = "gagspeak_hypnotic_effects_sent";
    public const string CounterKinkstersShocked = "gagspeak_kinksters_shocked";

    // Vibe Rooms
    public const string GaugeVibeRoomsActive = "gagspeak_vibe_rooms_active";
    public const string GaugeVibeRoomUsersActive = "gagspeak_vibe_room_users_active";
    public const string CounterVibeLobbySearches = "gagspeak_vibe_lobby_searches";
    public const string CounterVibeLobbiesCreated = "gagspeak_vibe_lobbies_created";
    public const string CounterVibeLobbiesJoined = "gagspeak_vibe_lobbies_joined";
    public const string CounterVibeLobbyDeviceUpdates = "gagspeak_vibe_lobby_device_updates";
    public const string CounterVibeLobbyChatsSent = "gagspeak_vibe_lobby_chats_sent";
}