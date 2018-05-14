using System;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private readonly RedisClient _client;
        
        public RedisDatabase()
        {
            _client = new RedisClient("127.0.0.1", 6379);;
        }

        public RedisDatabase(DatabaseConfig config)
        {
            _client = new RedisClient(config.IpAddress, config.Port);
        }
        
        public async Task<byte[]> GetAsync(string key, Type type)
        {
            return await Task.FromResult(_client.Get(key));
        }

        public async Task SetAsync(string key, byte[] bytes)
        {
            await Task.FromResult(_client.Set(key, bytes));
        }

        public bool IsConnected()
        {
            try
            {
                _client.Set("test", null);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}