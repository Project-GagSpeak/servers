namespace GagspeakServer.Metrics;

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

    public const string CounterUserPushData = "gagspeak_user_push";
    public const string CounterUserPushDataTo = "gagspeak_user_push_to";

    public const string CounterAuthenticationRequests = "gagspeak_auth_requests";
    public const string CounterAuthenticationCacheHits = "gagspeak_auth_requests_cachehit";
    public const string CounterAuthenticationFailures = "gagspeak_auth_requests_fail";
    public const string CounterAuthenticationSuccesses = "gagspeak_auth_requests_success";
    public const string GaugeAuthenticationCacheEntries = "gagspeak_auth_cache";

    public const string GaugeCurrentDownloads = "gagspeak_current_downloads";

    public const string GaugeQueueFree = "gagspeak_download_queue_free";
    public const string GaugeQueueActive = "gagspeak_download_queue_active";
    public const string GaugeQueueInactive = "gagspeak_download_queue_inactive";

    public const string CounterUserPairCacheHit = "gagspeak_pairscache_hit";
    public const string CounterUserPairCacheMiss = "gagspeak_pairscache_miss";

    public const string GaugeUserPairCacheUsers = "gagspeak_pairscache_users";
    public const string GaugeUserPairCacheEntries = "gagspeak_pairscache_entries";
    public const string CounterUserPairCacheNewEntries = "gagspeak_pairscache_new_entries";
    public const string CounterUserPairCacheUpdatedEntries = "gagspeak_pairscache_updated_entries";
}