using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Database.Config;
using NServiceKit.Redis;

namespace AElf.Database
{
    public class RedisDatabase : IKeyValueDatabase
    {
        private readonly PooledRedisClientManager _client;

        public RedisDatabase()
        {
            _client = new PooledRedisClientManager(DatabaseConfig.Instance.Number,
                $"{DatabaseConfig.Instance.Host}:{DatabaseConfig.Instance.Port}");
        }

        public async Task<byte[]> GetAsync(string key, Type type)
        {
            return await Task.FromResult(_client.GetCacheClient().Get<byte[]>(key));
        }

        public async Task SetAsync(string key, byte[] bytes)
        {
            await Task.FromResult(_client.GetCacheClient().Set(key, bytes));
        }

        public async Task<bool> PipelineSetAsync(IEnumerable<KeyValuePair<string, byte[]>> queue)
        {
            return await Task.Factory.StartNew(() =>
            {
                var keyValuePairs = queue as KeyValuePair<string, byte[]>[] ?? queue.ToArray();
                _client.GetCacheClient().SetAll(keyValuePairs.ToDictionary(x => x.Key, x => x.Value));
                Console.WriteLine("PipelineSetAsync::" + keyValuePairs.ToArray().Length);
                return true;
            });
        }

        public bool IsConnected()
        {
            try
            {
                _client.GetCacheClient().Set<byte[]>("ping", null);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}