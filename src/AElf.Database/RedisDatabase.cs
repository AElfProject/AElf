using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public async Task<bool> IsExists(string key)
        {
            Stopwatch stopwatch = null;
            try
            {
                stopwatch = Stopwatch.StartNew();
                Check.NotNullOrWhiteSpace(key, nameof(key));
                return _pooledRedisLite.Exists(key);
            }
            finally
            {
                stopwatch.Stop();
                Console.WriteLine($"## IsExists: {stopwatch.ElapsedMilliseconds}");
            }
        }

        public bool IsConnected()
        {
            Stopwatch stopwatch = null;
            try
            {
                stopwatch = Stopwatch.StartNew();
                return _pooledRedisLite.Ping();
            }
            finally
            {
                stopwatch.Stop();
                Console.WriteLine($"## IsConnected: {stopwatch.ElapsedMilliseconds}");
            }
        }

        public async Task<byte[]> GetAsync(string key)
        {
            Stopwatch stopwatch = null;
            try
            {
                stopwatch = Stopwatch.StartNew();
                Check.NotNullOrWhiteSpace(key, nameof(key));
                return _pooledRedisLite.Get(key);
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
            _pooledRedisLite.Set(key, bytes);
            stopwatch.Stop();
            Console.WriteLine($"## SetAsync: {stopwatch.ElapsedMilliseconds}");
        }

        public async Task RemoveAsync(string key)
        {
            var stopwatch = Stopwatch.StartNew();
            Check.NotNullOrWhiteSpace(key, nameof(key));
            _pooledRedisLite.Remove(key);
            stopwatch.Stop();
            Console.WriteLine($"## RemoveAsync: {stopwatch.ElapsedMilliseconds}");
        }

        public async Task SetAllAsync(Dictionary<string, byte[]> cache)
        {
            var stopwatch = Stopwatch.StartNew();
            if (cache.Count == 0)
                return;
            _pooledRedisLite.SetAll(cache);
            stopwatch.Stop();
            Console.WriteLine($"## SetAllAsync: {stopwatch.ElapsedMilliseconds}");
        }
    }
}