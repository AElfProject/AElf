using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Database.RedisProtocol;
using NServiceKit.Redis;
using Volo.Abp;

namespace AElf.Database
{
    public class RedisDatabase<TKeyValueDbContext> : IKeyValueDatabase<TKeyValueDbContext>
        where TKeyValueDbContext : KeyValueDbContext<TKeyValueDbContext>
    {
        private readonly PooledRedisClientManager _pooledRedisLite;

        public RedisDatabase(KeyValueDatabaseOptions<TKeyValueDbContext> options)
        {
            Check.NotNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));

            var endpoint = options.ConnectionString.ToRedisEndpoint();

            _pooledRedisLite = new PooledRedisClientManager($"{endpoint.Host}:{endpoint.Port}");
        }

        public bool IsConnected()
        {
            return _pooledRedisLite.GetCacheClient().Set("Ping", "");
        }


        public async Task<byte[]> GetAsync(string key)
        {
            Check.NotNullOrWhiteSpace(key, nameof(key));
            return await Task.Run(() => _pooledRedisLite.GetCacheClient().Get<byte[]>(key));
        }

        public async Task SetAsync(string key, byte[] bytes)
        {
            Check.NotNullOrWhiteSpace(key, nameof(key));

            await Task.Run(() => _pooledRedisLite.GetCacheClient().Set(key, bytes));
        }

        public async Task RemoveAsync(string key)
        {
            Check.NotNullOrWhiteSpace(key, nameof(key));

            await Task.Run(() => _pooledRedisLite.GetCacheClient().Remove(key));
        }

        public async Task PipelineSetAsync(Dictionary<string, byte[]> cache)
        {
            if (cache.Count == 0)
            {
                return;
            }

            await Task.Run(() =>
            {
                _pooledRedisLite.GetCacheClient().SetAll(cache);
                return true;
            });
        }
    }
}