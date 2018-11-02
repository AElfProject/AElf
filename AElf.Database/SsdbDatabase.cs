using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Configuration;
using NServiceKit.CacheAccess;
using NServiceKit.Redis;

namespace AElf.Database
{
    public class SsdbDatabase : IKeyValueDatabase
    {
        //private readonly PooledRedisClientManager _client;
        
        private readonly ConcurrentDictionary<string, PooledRedisClientManager> _clientManagers = new ConcurrentDictionary<string, PooledRedisClientManager>();

        public SsdbDatabase()
        {
            //_client = new PooledRedisClientManager($"{DatabaseConfig.Instance.Host}:{DatabaseConfig.Instance.Port}");
        }

        public async Task<byte[]> GetAsync(string database, string key)
        {
            return await Task.FromResult(GetClient(database).Get<byte[]>(key));
        }

        public async Task SetAsync(string database, string key, byte[] bytes)
        {
            await Task.FromResult(GetClient(database).Set(key, bytes));
        }

        public async Task RemoveAsync(string database, string key)
        {
            await Task.FromResult(GetClient(database).Remove(key));
        }

        public async Task<bool> PipelineSetAsync(string database, Dictionary<string, byte[]> cache)
        {
            if (cache.Count == 0)
            {
                return true;
            }
            return await Task.Factory.StartNew(() =>
            {
                GetClient(database).SetAll(cache);
                return true;
            });
        }

        public bool IsConnected(string database = "")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(database))
                {
                    foreach (var pooledRedisClientManager in _clientManagers.Values)
                    {
                        pooledRedisClientManager.GetCacheClient().Set<byte[]>("ping", null);
                    }
                }
                else
                {
                    GetClient(database).Set<byte[]>("ping", null);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        
        private ICacheClient GetClient(string database)
        {
            if (string.IsNullOrWhiteSpace(database))
            {
                throw new ArgumentException("database is empty");
            }
            database = database.ToLower();
            if (!_clientManagers.TryGetValue(database.ToLower(), out var client))
            {
                // get from config
                client = new PooledRedisClientManager();
                _clientManagers.TryAdd(database, client);
            }

            return client.GetCacheClient();
        }
    }
}