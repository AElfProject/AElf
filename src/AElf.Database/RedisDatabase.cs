using System.Collections.Generic;
using System.Linq;
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

        public async Task<bool> IsExistsAsync(string key)
        {
            Check.NotNullOrWhiteSpace(key, nameof(key));
            return _pooledRedisLite.Exists(key);
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

        public async Task SetAllAsync(IDictionary<string, byte[]> values)
        {
            if (values.Count == 0)
                return;
            foreach (var key in values.Keys)
            {
                Check.NotNullOrWhiteSpace(key, nameof(key));
            }
            _pooledRedisLite.SetAll(values);
        }
        
        public async Task<List<byte[]>> GetAllAsync(IList<string> keys)
        {
            if (keys.Count == 0)
                return null;
            foreach (var key in keys)
            {
                Check.NotNullOrWhiteSpace(key, nameof(key));
            }

            return _pooledRedisLite.GetAll(keys.ToArray()).ToList();
        }
        
        public async Task RemoveAllAsync(IList<string> keys)
        {
            if (keys.Count == 0)
                return;
            foreach (var key in keys)
            {
                Check.NotNullOrWhiteSpace(key, nameof(key));
            }

            _pooledRedisLite.RemoveAll(keys.ToArray());
        }
    }
}