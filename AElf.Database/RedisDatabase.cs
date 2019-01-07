using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Database.RedisProtocol;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Threading;

namespace AElf.Database
{
    public class RedisDatabase<TKeyValueDbContext> : IKeyValueDatabase<TKeyValueDbContext>
        where TKeyValueDbContext:KeyValueDbContext<TKeyValueDbContext>
    {
        private readonly PooledRedisLite _pooledRedisLite;

        public RedisDatabase(KeyValueDatabaseOptions<TKeyValueDbContext> options)
        {
            var endpoint = options.ConnectionString.ToRedisEndpoint();
            
            _pooledRedisLite = new PooledRedisLite(endpoint.Host,endpoint.Port,(int)endpoint.Db);
        }

        public bool IsConnected()
        {
            return _pooledRedisLite.Ping();
        }


        public async Task<byte[]> GetAsync(string key)
        {
            Check.NotNullOrWhiteSpace(key,nameof(key));
            return await Task.Run(() => _pooledRedisLite.Get(key));
        }

        public async Task<bool> SetAsync(string key, byte[] bytes)
        {
            Check.NotNullOrWhiteSpace(key,nameof(key));
            
            return await Task.Run(() => _pooledRedisLite.Set(key,bytes));
        }

        public async Task<bool> RemoveAsync(string key)
        {
            Check.NotNullOrWhiteSpace(key,nameof(key));

            return await Task.Run(() => _pooledRedisLite.Remove(key));
        }

        public async Task<bool> PipelineSetAsync(Dictionary<string, byte[]> cache)
        {
            if (cache.Count == 0)
            {
                return true;
            }
            return await Task.Run(() =>
            {
                _pooledRedisLite.SetAll(cache);
                return true;
            });
        }

    }
}