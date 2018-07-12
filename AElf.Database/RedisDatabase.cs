using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Database.Config;
using StackExchange.Redis;

namespace AElf.Database
{
    public class RedisDatabase : IKeyValueDatabase
    {
        private readonly ConnectionMultiplexer _client;

        private readonly IDatabase _database;

        public RedisDatabase()
        {
            _client = ConnectionMultiplexer.Connect($"{DatabaseConfig.Instance.Host}:{DatabaseConfig.Instance.Port}");
            _database = _client.GetDatabase(DatabaseConfig.Instance.Number);
        }

        public async Task<byte[]> GetAsync(string key, Type type)
        {
            return await Task.FromResult(_database.StringGet(key));
        }

        public async Task SetAsync(string key, byte[] bytes)
        {
            await Task.FromResult(_database.StringSet(key, bytes));
        }

        public async Task<bool> PipelineSetAsync(IEnumerable<KeyValuePair<string, byte[]>> queue)
        {
            var batch = _database.CreateBatch();
            var task = await batch.StringSetAsync(queue.ToDictionary(kv => (RedisKey) kv.Key, kv => (RedisValue) kv.Value).ToArray());
            batch.Execute();
            return task;
        }

        public bool IsConnected()
        {
            try
            {
                _database.Ping();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}