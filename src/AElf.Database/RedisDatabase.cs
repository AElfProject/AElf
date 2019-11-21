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
        
        public Task<bool> IsExists(string key)
        {
            Check.NotNullOrWhiteSpace(key, nameof(key));
            return Task.FromResult(_connectionMultiplexer.GetDatabase().KeyExists(key));
        }

        public Task<byte[]> GetAsync(string key)
        {
            Check.NotNullOrWhiteSpace(key, nameof(key));
            var value = _connectionMultiplexer.GetDatabase().StringGet(key);
            return Task.FromResult<byte[]>(value);
        }

        public Task SetAsync(string key, byte[] bytes)
        {
            Check.NotNullOrWhiteSpace(key, nameof(key));
            _connectionMultiplexer.GetDatabase().StringSet(key, bytes);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            Check.NotNullOrWhiteSpace(key, nameof(key));
            _connectionMultiplexer.GetDatabase().KeyDelete(key);
            return Task.CompletedTask;
        }

        public Task SetAllAsync(Dictionary<string, byte[]> cache)
        {
            if (cache.Count == 0)
                return Task.CompletedTask;
            var keyPairs = cache.Select(entry => new KeyValuePair<RedisKey, RedisValue>(entry.Key, entry.Value));
            _connectionMultiplexer.GetDatabase().StringSet(keyPairs.ToArray());
            
            return Task.CompletedTask;
        }
    }
}