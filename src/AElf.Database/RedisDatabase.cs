using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
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
            Stopwatch stopwatch = null;
            try
            {
                stopwatch = Stopwatch.StartNew();
                return _connectionMultiplexer.IsConnected;
            }
            finally
            {
                stopwatch.Stop();
                Console.WriteLine($"## IsConnected: {stopwatch.ElapsedMilliseconds}");
            }
        }
        
        public async Task<bool> IsExists(string key)
        {
            Stopwatch stopwatch = null;
            try
            {
                stopwatch = Stopwatch.StartNew();
                Check.NotNullOrWhiteSpace(key, nameof(key));
                return await _connectionMultiplexer.GetDatabase().KeyExistsAsync(key);
            }
            finally
            {
                stopwatch.Stop();
                Console.WriteLine($"## IsExists: {stopwatch.ElapsedMilliseconds}");
            }
            
        }

        public async Task<byte[]> GetAsync(string key)
        {
            Stopwatch stopwatch = null;
            try
            {
                stopwatch = Stopwatch.StartNew();
                Check.NotNullOrWhiteSpace(key, nameof(key));
                return await _connectionMultiplexer.GetDatabase().StringGetAsync(key);
            }
            finally
            {
                stopwatch.Stop();
                Console.WriteLine($"## GetAsync: {stopwatch.ElapsedMilliseconds}");
            }
            
        }

        public async Task SetAsync(string key, byte[] bytes)
        {
            var stopwatch = Stopwatch.StartNew();
            Check.NotNullOrWhiteSpace(key, nameof(key));
            await _connectionMultiplexer.GetDatabase().StringSetAsync(key, bytes);
            stopwatch.Stop();
            Console.WriteLine($"## SetAsync: {stopwatch.ElapsedMilliseconds}");
        }

        public async Task RemoveAsync(string key)
        {
            var stopwatch = Stopwatch.StartNew();
            Check.NotNullOrWhiteSpace(key, nameof(key));
            await _connectionMultiplexer.GetDatabase().KeyDeleteAsync(key);
            stopwatch.Stop();
            Console.WriteLine($"## RemoveAsync: {stopwatch.ElapsedMilliseconds}");
        }

        public async Task SetAllAsync(Dictionary<string, byte[]> cache)
        {
            var stopwatch = Stopwatch.StartNew();
            if (cache.Count == 0)
                return;
            var keyPairs = cache.Select(entry => new KeyValuePair<RedisKey, RedisValue>(entry.Key, entry.Value));
            await _connectionMultiplexer.GetDatabase().StringSetAsync(keyPairs.ToArray());
            stopwatch.Stop();
            Console.WriteLine($"## SetAllAsync: {stopwatch.ElapsedMilliseconds}");
        }
    }
}