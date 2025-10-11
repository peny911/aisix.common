using SqlSugar;
using System.Runtime.Caching;

namespace Aisix.Common.Db.Cache
{
    public class SqlSugarMemoryCache : ICacheService
    {
        public void Add<V>(string key, V value)
        {
            if (value == null) return;

            MemoryCache.Default[key] = value;
        }

        public void Add<V>(string key, V value, int cacheDurationInSeconds)
        {
            if (value == null) return;

            MemoryCache.Default.Add(
                key,
                value,
                new CacheItemPolicy { AbsoluteExpiration = DateTime.Now.AddSeconds(cacheDurationInSeconds) });
        }

        public bool ContainsKey<V>(string key)
        {
            return MemoryCache.Default.Contains(key);
        }

        public V Get<V>(string key)
        {
            return (V)MemoryCache.Default.Get(key);
        }

        public IEnumerable<string> GetAllKey<V>()
        {
            return MemoryCache.Default.Select(c => c.Key).ToList();
        }

        public V GetOrCreate<V>(string cacheKey, Func<V> create, int cacheDurationInSeconds = int.MaxValue)
        {
            var memoryCache = MemoryCache.Default;
            if (memoryCache.Contains(cacheKey))
            {
                return (V)memoryCache[cacheKey];
            }

            var result = create();
            memoryCache.Add(
                cacheKey,
                result,
                new CacheItemPolicy { AbsoluteExpiration = DateTime.Now.AddSeconds(cacheDurationInSeconds) });
            return result;
        }

        public void Remove<V>(string key)
        {
            MemoryCache.Default.Remove(key);
        }
    }
}
