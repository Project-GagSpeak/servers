namespace GagspeakShared.Metrics;

/// <summary> Fun Metrics about Gagspeak, maybe add more later, who knows! </summary>
public class MetricsAPI
{
    public const string CounterInitializedConnections = "gagspeak_initialized_connections";
    public const string GaugeConnections = "gagspeak_connections";
    public const string GaugeAuthorizedConnections = "gagspeak_authorized_connections";

    public const string GaugeAvailableWorkerThreads = "gagspeak_available_threadpool";
    public const string GaugeAvailableIOWorkerThreads = "gagspeak_available_threadpool_io";

    public const string GaugeUsersRegistered = "gagspeak_users_registered";
    public const string CounterUsersRegisteredDeleted = "gagspeak_users_registered_deleted";
    public const string GaugePairs = "gagspeak_pairs";
    public const string GaugePairsPaused = "gagspeak_pairs_paused";

    public const string CounterUserPushDataComposite = "gagspeak_user_push_composite";
    public const string CounterUserPushDataIpc = "gagspeak_user_push_ipc";
    public const string CounterUserPushDataAppearance = "gagspeak_user_push_appearance";
    public const string CounterUserPushDataWardrobe = "gagspeak_user_push_wardrobe";
    public const string CounterUserPushDataAlias = "gagspeak_user_push_alias";
    public const string CounterUserPushDataToybox = "gagspeak_user_push_toybox";
    public const string CounterUserPushDataPiShock = "gagspeak_user_push_pishock";


    public const string CounterUserPushDataCompositeTo = "gagspeak_user_push_composite_to";
    public const string CounterUserPushDataIpcTo = "gagspeak_user_push_ipc_to";
    public const string CounterUserPushDataAppearanceTo = "gagspeak_user_push_appearance_to";
    public const string CounterUserPushDataWardrobeTo = "gagspeak_user_push_wardrobe_to";
    public const string CounterUserPushDataAliasTo = "gagspeak_user_push_alias_to";
    public const string CounterUserPushDataToyboxTo = "gagspeak_user_push_toybox_to";
    public const string CounterUserPushDataPiShockTo = "gagspeak_user_push_pishock_to";

    public const string CounterAuthenticationRequests = "gagspeak_auth_requests";
    public const string CounterAuthenticationCacheHits = "gagspeak_auth_requests_cachehit";
    public const string CounterAuthenticationFailures = "gagspeak_auth_requests_fail";
    public const string CounterAuthenticationSuccesses = "gagspeak_auth_requests_success";
    public const string GaugeAuthenticationCacheEntries = "gagspeak_auth_cache";

    public const string CounterUserPairCacheHit = "gagspeak_pairscache_hit";
    public const string CounterUserPairCacheMiss = "gagspeak_pairscache_miss";

    public const string GaugeUserPairCacheUsers = "gagspeak_pairscache_users";
    public const string GaugeUserPairCacheEntries = "gagspeak_pairscache_entries";
    public const string CounterUserPairCacheNewEntries = "gagspeak_pairscache_new_entries";
    public const string CounterUserPairCacheUpdatedEntries = "gagspeak_pairscache_updated_entries";
}