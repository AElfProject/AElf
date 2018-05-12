using System;
using System.Reflection;
using System.Threading.Tasks;
using ServiceStack.Redis;

namespace AElf.Database
{
    public static class RedisHelper
    {
        private const string IpAddress = "127.0.0.1";
        private const int Port = 6379;

        private static RedisClient RedisClient => new RedisClient(IpAddress, Port);

        public static Task<bool> SetAsync(string key, byte[] bytes)
        {
            return Task.FromResult(RedisClient.Set(key, bytes));
        }

        public static Task<byte[]> GetAsync(string key)
        {
            return Task.FromResult(RedisClient.Get(key));
        }
        
        public static RedisClient GetRedisClient()
        {
            return new RedisClient(IpAddress, Port);
        }

        public static RedisClient GetRedisClient(int port)
        {
            return new RedisClient(IpAddress, port);
        }
        
        public static RedisClient GetRedisClient(string ipAddress, int port)
        {
            return new RedisClient(ipAddress, port);
        }
    }
}