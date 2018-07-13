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
        private readonly RedisClient _client2;

        public RedisDatabase()
        {
            _client = new PooledRedisClientManager(DatabaseConfig.Instance.Number,
                $"{DatabaseConfig.Instance.Host}:{DatabaseConfig.Instance.Port}");
            _client2 = new RedisClient(DatabaseConfig.Instance.Host, DatabaseConfig.Instance.Port, null,
                DatabaseConfig.Instance.Number);
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
                _client.GetCacheClient().SetAll(queue.ToDictionary(x => x.Key, x => x.Value));
                return true;
            });

//            return await Task.Factory.StartNew(() =>
//            {
//                var queueList = queue.ToArray();
//                if (queueList.Length == 0)
//                    return true;
//                
//                Console.WriteLine("PipelineSetAsync::" + queueList.Length);
//                using (var pipline = _client2.CreatePipeline())
//                {
//                    foreach (var item in queueList)
//                    {
////                        _client.GetCacheClient().Set(item.Key, item.Value);
//                        pipline.QueueCommand(p => p.Set(item.Key, item.Value));
//                    }
//                    pipline.Flush();
//                }
//
//                return true;
//            });
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