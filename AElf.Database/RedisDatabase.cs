using System;
using System.Threading.Tasks;
using ServiceStack.Redis;

namespace AElf.Database
{
    public class RedisDatabase : IKeyValueDatabase
    {
        private readonly IRedisClient _client;
        
        public RedisDatabase():this(new DatabaseConfig())
        {
        }

        public RedisDatabase(DatabaseConfig config)
        {
            using (var redisManager = new RedisManagerPool($"{config.IpAddress}:{config.Port}"))
            {
                _client = redisManager.GetClient();
            }
        }
        
        public async Task<byte[]> GetAsync(string key, Type type)
        {
            return await Task.FromResult(_client.Get<byte[]>(key));
        }

        public async Task SetAsync(string key, byte[] bytes)
        {
            await Task.FromResult(_client.Set(key, bytes));
        }

        public bool IsConnected()
        {
            try
            {
                _client.Set<byte[]>("test", null);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}