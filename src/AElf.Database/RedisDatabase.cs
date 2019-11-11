using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;
using Volo.Abp;

namespace AElf.Database
{
    public class RedisDatabase<TKeyValueDbContext> : IKeyValueDatabase<TKeyValueDbContext>
        where TKeyValueDbContext : KeyValueDbContext<TKeyValueDbContext>
    {
        private static ConnectionMultiplexer _connectionMultiplexer;

        public RedisDatabase(KeyValueDatabaseOptions<TKeyValueDbContext> options)
        {
            Check.NotNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
            var endpoint = DatabaseEndpoint.ParseFromConnectionString(options.ConnectionString);
            var config = new ConfigurationOptions
            {
                EndPoints = {{endpoint.Host, endpoint.Port}},
                DefaultDatabase = endpoint.DatabaseNumber,
                CommandMap = CommandMap.Create(new HashSet<string> {"SELECT", "SET", "MSET", "GET", "EXISTS", "DEL"})
            };
            _connectionMultiplexer = ConnectionMultiplexer.Connect(config);
        }

        public bool IsConnected()
        {
            return _connectionMultiplexer.IsConnected;
        }
        
        public async Task<bool> IsExists(string key)
        {
            Check.NotNullOrWhiteSpace(key, nameof(key));
            return await _connectionMultiplexer.GetDatabase().KeyExistsAsync(key);
        }

        public async Task<byte[]> GetAsync(string key)
        {
            Check.NotNullOrWhiteSpace(key, nameof(key));
            return await _connectionMultiplexer.GetDatabase().StringGetAsync(key);
        }

        public async Task SetAsync(string key, byte[] bytes)
        {
            Check.NotNullOrWhiteSpace(key, nameof(key));
            await _connectionMultiplexer.GetDatabase().StringSetAsync(key, bytes);
        }

        public async Task RemoveAsync(string key)
        {
            Check.NotNullOrWhiteSpace(key, nameof(key));
            await _connectionMultiplexer.GetDatabase().KeyDeleteAsync(key);
        }

        public async Task SetAllAsync(Dictionary<string, byte[]> cache)
        {
            if (cache.Count == 0)
                return;
            var keyPairs = cache.Select(entry => new KeyValuePair<RedisKey, RedisValue>(entry.Key, entry.Value));
            await _connectionMultiplexer.GetDatabase().StringSetAsync(keyPairs.ToArray());
        }
    }
}