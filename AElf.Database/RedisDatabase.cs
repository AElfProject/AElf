using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using AElf.Database;
using ServiceStack;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace AElf.Database
{
    public class RedisDatabase : IKeyValueDatabase
    {
        private readonly IRedisClient _client;
        
        public RedisDatabase()
        {
            using (var redisManager = new RedisManagerPool("127.0.0.1:6379"))
            {
                _client = redisManager.GetClient();
            }
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