using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Collections;
using System.Collections.Concurrent;
using System.Net;

namespace Collecto.CoreAPI.Helper
{
    /// <summary>
    /// Customized cache, it may be "Distributed Cache (Redis)" or "Memory Cache",
    /// it depends on value in appsettings.json file.    
    /// If the value of "RedisUrl" field is present it will use "Distributed Cache (Redis)"
    /// otherwise it will use "Memory Cache". 
    /// </summary>
    public interface ICollectoCache : IEnumerable<KeyValuePair<string, object>>
    {
        /// <summary>
        /// Get cached object
        /// </summary>
        /// <typeparam name="T">Any type of object</typeparam>
        /// <param name="key">Key to retrieve data</param>
        /// <param name="value">Instance of an object type T</param>
        /// <returns>True if successful otherwise false</returns>
        bool TryGetValue<T>(string key, out T value);

        /// <summary>
        /// Set object to cache
        /// </summary>
        /// <typeparam name="T">Any type of object</typeparam>
        /// <param name="key">Key to set data to cache</param>
        /// <param name="value">Instance of an object type T</param>
        /// <param name="options">How many time to keep in cache</param>
        /// <returns>True if successful otherwise false</returns>
        bool Set<T>(string key, T value, DateTimeOffset options);

        /// <summary>
        /// Delete caches of matched pattern
        /// </summary>
        /// <param name="pattern">Pattern to search</param>
        void Clear(string pattern);
    }

    /// <summary>
    /// 
    /// </summary>
    public class CollectoCache : ICollectoCache
    {
        private readonly IDatabase _db;
        private readonly IServer _server;
        private readonly bool _redisCacheEnbled;
        private readonly IMemoryCache _memoryCache;
        private readonly ConcurrentDictionary<string, ICacheEntry> _cacheEntries = new();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="memoryCache"></param>
        public CollectoCache(IMemoryCache memoryCache)
        {
            _redisCacheEnbled = CacheConnection.RedisCacheEnbled;
            if (_redisCacheEnbled)
            {
                _db = CacheConnection.Connection.GetDatabase();
                EndPoint[] endpoints = CacheConnection.Connection.GetEndPoints(configuredOnly: true);
                foreach (EndPoint endpoint in endpoints)
                {
                    _server = CacheConnection.Connection.GetServer(endpoint);
                }
            }
            else
            {
                _memoryCache = memoryCache;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public bool Set<T>(string key, T value, DateTimeOffset options)
        {
            if (_redisCacheEnbled)
            {
                TimeSpan expiryTime = options.DateTime.Subtract(DateTime.Now);
                return _db.StringSet(key: key, value: JsonConvert.SerializeObject(value), expiry: expiryTime);
            }
            else
            {
                using ICacheEntry entry = _memoryCache.CreateEntry(key);
                entry.RegisterPostEvictionCallback(PostEvictionCallback);
                _cacheEntries.AddOrUpdate(key: key, addValue: entry, (o, cacheEntry) =>
                {
                    cacheEntry.Value = entry;
                    return cacheEntry;
                });
                entry.AbsoluteExpiration = options;
                entry.Value = value;

                return true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="reason"></param>
        /// <param name="state"></param>
        private void PostEvictionCallback(object key, object value, EvictionReason reason, object state)
        {
            if (_redisCacheEnbled == false && reason != EvictionReason.Replaced)
                _cacheEntries.TryRemove(key: key.ToString(), value: out _);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue<T>(string key, out T value)
        {
            if (_redisCacheEnbled)
            {
                RedisValue keValue = _db.StringGet(key);
                if (string.IsNullOrEmpty(keValue))
                {
                    value = default;
                    return false;
                }

                value = JsonConvert.DeserializeObject<T>(keValue);
                return true;
            }
            else
            {
                return _memoryCache.TryGetValue(key: key, value: out value);
            }
        }

        /// <summary>
        /// Delete caches of matched pattern
        /// </summary>
        /// <param name="pattern">Pattern to search</param>
        public void Clear(string pattern)
        {
            if (_redisCacheEnbled)
            {
                foreach (RedisKey key in _server.Keys(pattern: $"*{pattern}*"))
                {
                    _db.KeyDelete(key: key);
                }
            }
            else
            {
                IEnumerable<string> keys = _cacheEntries.Keys.Where(key => key.IndexOf(pattern) != -1);
                foreach (string key in keys)
                {
                    _memoryCache.Remove(key: key);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _cacheEntries.Select(pair => new KeyValuePair<string, object>(pair.Key, pair.Value.Value)).GetEnumerator();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            if (_redisCacheEnbled == false)
                _memoryCache.Dispose();
        }
    }
}
