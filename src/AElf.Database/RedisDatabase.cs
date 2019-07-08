using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Database.RedisProtocol;
using Volo.Abp;

#pragma warning disable 1998

namespace AElf.Database
{
    public class RedisDatabase<TKeyValueDbContext> : IKeyValueDatabase<TKeyValueDbContext>
        where TKeyValueDbContext : KeyValueDbContext<TKeyValueDbContext>
    {
        private readonly PooledRedisLite _pooledRedisLite;

        public RedisDatabase(KeyValueDatabaseOptions<TKeyValueDbContext> options)
        {
            Check.NotNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
            var endpoint = options.ConnectionString.ToRedisEndpoint();
            _pooledRedisLite = new PooledRedisLite(endpoint.Host, endpoint.Port, db: (int) endpoint.Db);
        }

        public bool IsConnected()
        {
            return _pooledRedisLite.Ping();
        }

        public async Task<byte[]> GetAsync(string key)
        {
            Check.NotNullOrWhiteSpace(key, nameof(key));
            return _pooledRedisLite.Get(key);
        }

        public async Task SetAsync(string key, byte[] bytes)
        {
            Check.NotNullOrWhiteSpace(key, nameof(key));
            _pooledRedisLite.Set(key, bytes);
        }

        public async Task RemoveAsync(string key)
        {
            Check.NotNullOrWhiteSpace(key, nameof(key));
            _pooledRedisLite.Remove(key);
        }

        public async Task SetAllAsync(Dictionary<string, byte[]> cache)
        {
            if (cache.Count == 0)
                return;
            _pooledRedisLite.SetAll(cache);
        }
    }
}