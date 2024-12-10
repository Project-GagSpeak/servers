namespace GagspeakServer.Services;

/// <summary>
/// The Cache Service for handling all currently online synced pairs active on the server
/// </summary>
public class OnlineSyncedPairCacheService
{
    private readonly Dictionary<string, PairCache> _lastSeenCache = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _cacheModificationSemaphore = new(1);
    private readonly ILogger<OnlineSyncedPairCacheService> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public OnlineSyncedPairCacheService(ILogger<OnlineSyncedPairCacheService> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    /// <summary> Initializes a player in the cache as a key.
    /// <para> Their value is then send to the last created cache for that players pair request</para>
    /// </summary>
    public async Task InitPlayer(string user)
    {
        if (_lastSeenCache.ContainsKey(user)) return;

        await _cacheModificationSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            _logger.LogDebug("Initializing {user}", user);
            _lastSeenCache[user] = new(_loggerFactory.CreateLogger<PairCache>(), user);
        }
        finally
        {
            _cacheModificationSemaphore.Release();
        }
    }

    /// <summary> Disposes of a player in the cache
    /// <para> If the player is not in the cache, it will return</para>
    /// </summary>
    public async Task DisposePlayer(string user)
    {
        if (!_lastSeenCache.ContainsKey(user)) return;

        await _cacheModificationSemaphore.WaitAsync().ConfigureAwait(false);
        try
        {
            _logger.LogDebug("Disposing {user}", user);
            _lastSeenCache.Remove(user, out var pairCache);
            pairCache?.Dispose();
        }
        finally
        {
            _cacheModificationSemaphore.Release();
        }
    }

    /// <summary> Checks if all players are cached in the cache service </summary>
    /// <returns> A boolean value of whether all players are cached or not </returns>
    public async Task<bool> AreAllPlayersCached(string sender, List<string> uids, CancellationToken ct)
    {
        if (!_lastSeenCache.ContainsKey(sender)) await InitPlayer(sender).ConfigureAwait(false);

        _lastSeenCache.TryGetValue(sender, out var pairCache);
        if (pairCache is null) return false;

        return await pairCache.AreAllPlayersCached(uids, ct).ConfigureAwait(false);
    }

    /// <summary> Caches all players in the cache service </summary>
    public async Task CachePlayers(string sender, List<string> uids, CancellationToken ct)
    {
        if (!_lastSeenCache.ContainsKey(sender)) await InitPlayer(sender).ConfigureAwait(false);

        _lastSeenCache.TryGetValue(sender, out var pairCache);
        if (pairCache is null) return;

        await pairCache.CachePlayers(uids, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// A sealed class representing a players pair cache
    /// </summary>
    private sealed class PairCache : IDisposable
    {
        private readonly ILogger<PairCache> _logger;
        private readonly string _owner;
        private readonly Dictionary<string, DateTime> _lastSeenCache = new(StringComparer.Ordinal);
        private readonly SemaphoreSlim _lock = new(1);

        public PairCache(ILogger<PairCache> logger, string owner)
        {
            _logger = logger;
            _owner = owner;
        }

        /// <summary> Checks if all players are cached in the cache service </summary>
        /// <returns> A boolean value of whether all players are cached or not </returns>
        public async Task<bool> AreAllPlayersCached(List<string> uids, CancellationToken ct)
        {
            await _lock.WaitAsync(ct).ConfigureAwait(false);

            try
            {
                var allCached = uids.TrueForAll(u => _lastSeenCache.TryGetValue(u, out var expiry) && expiry > DateTime.UtcNow);

                _logger.LogDebug("AreAllPlayersCached:{uid}:{count}:{result}", _owner, uids.Count, allCached);

                return allCached;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary> Caches all players in the cache service </summary>
        public async Task CachePlayers(List<string> uids, CancellationToken ct)
        {
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var lastSeen = DateTime.UtcNow.AddMinutes(60);
                _logger.LogDebug("CacheOnlinePlayers:{uid}:{count}", _owner, uids.Count);
                var newEntries = uids.Count(u => !_lastSeenCache.ContainsKey(u));

                uids.ForEach(u => _lastSeenCache[u] = lastSeen);

                // clean up old entries
                var outdatedEntries = _lastSeenCache.Where(u => u.Value < DateTime.UtcNow).Select(k => k.Key).ToList();
                if (outdatedEntries.Any())
                {
                    foreach (var entry in outdatedEntries)
                    {
                        _lastSeenCache.Remove(entry);
                    }
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary> Disposes of the pair cache </summary>
        public void Dispose()
        { 
            _lock.Dispose();
        }
    }
}
