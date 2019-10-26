using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Database.RedisProtocol;
using StackExchange.Redis;
using Volo.Abp;

#pragma warning disable 1998

namespace AElf.Database
{
    public class RedisDatabase<TKeyValueDbContext> : IKeyValueDatabase<TKeyValueDbContext>
        where TKeyValueDbContext : KeyValueDbContext<TKeyValueDbContext>
    {
        private readonly ConnectionMultiplexer _connectionMultiplexer;

        public RedisDatabase(KeyValueDatabaseOptions<TKeyValueDbContext> options)
        {
            Check.NotNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
            var endpoint = options.ConnectionString.ToRedisEndpoint();
            var config = new ConfigurationOptions
            {
                EndPoints = { { endpoint.Host, endpoint.Port } },
                DefaultDatabase = (int)endpoint.Db
            };

            _connectionMultiplexer = ConnectionMultiplexer.Connect(config);

        }

        public bool IsConnected()
        {
            return _connectionMultiplexer.IsConnected;
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

            var keyPairs = cache.Select(entry => new KeyValuePair<RedisKey, RedisValue>(entry.Key, entry.Value))
                .ToArray();

            await _connectionMultiplexer.GetDatabase().StringSetAsync(keyPairs);
        }
    }
}