using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Configuration;
using NServiceKit.CacheAccess;
using NServiceKit.Redis;

namespace AElf.Database
{
    public class RedisDatabase : IKeyValueDatabase
    {
        private readonly ConcurrentDictionary<string, PooledRedisClientManager> _clientManagers = new ConcurrentDictionary<string, PooledRedisClientManager>();

        public async Task<byte[]> GetAsync(string database, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("key is empty");
            }

            return await Task.FromResult(GetClient(database).Get<byte[]>(key));
        }

        public async Task SetAsync(string database, string key, byte[] bytes)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("key is empty");
            }

            await Task.FromResult(GetClient(database).Set(key, bytes));
        }

        public async Task RemoveAsync(string database, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("key is empty");
            }
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
                    foreach (var db in DatabaseConfig.Instance.Hosts)
                    {
                        GetClient(db.Key).Set<byte[]>("ping", null);
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
            if (!_clientManagers.TryGetValue(database, out var client))
            {
                var databaseHost = DatabaseConfig.Instance.GetHost(database);
                client = new PooledRedisClientManager(databaseHost.Number,$"{databaseHost.Host}:{databaseHost.Port}");
                _clientManagers.TryAdd(database, client);
            }

            return client.GetCacheClient();
        }
    }
}