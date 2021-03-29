using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace AElf.Database
{
    public class SCRedisLite
    {
        private ConnectionMultiplexer redis;

        public SCRedisLite(string host, int port = 6379, string password = null, int db = 0, int poolSize = 20)
        {
            redis =
                ConnectionMultiplexer.Connect(string.Join(":", new[] {host, port.ToString()}));
        }

        public async Task<byte[]> GetAsync(string key)
        {
            var redisLite = GetLite();
            byte[] v = await redisLite.StringGetAsync(key);
            return v;
        }

        public async Task<bool> ExistsAsync(string key)
        {
            var redisLite = GetLite();
            return await redisLite.KeyExistsAsync(key);
        }

        public async Task<bool> PingAsync()
        {
            var redisLite = GetLite();
            return (await redisLite.PingAsync()).TotalMilliseconds < 500;
        }


        public async Task<bool> SetAsync(string key, byte[] value)
        {
            var redisLite = GetLite();
            return await redisLite.StringSetAsync(key, value);
        }

        public async Task SetAllAsync(IDictionary<string, byte[]> dict)
        {
            var redisLite = GetLite();
            // dict.
            await redisLite.StringSetAsync(dict
                .Select(pair => new KeyValuePair<RedisKey, RedisValue>(pair.Key, pair.Value))
                .ToArray());
        }

        public async Task<bool> RemoveAsync(string key)
        {
            var redisLite = GetLite();
            return await redisLite.KeyDeleteAsync(key);
        }

        public async Task<bool> RemoveAllAsync(string[] key)
        {
            var redisLite = GetLite();
            return await redisLite.KeyDeleteAsync(key.Select(k => (RedisKey) k).ToArray()) == key.Length;
        }

        public byte[] Get(string key)
        {
            var redisLite = GetLite();
            byte[] v = redisLite.StringGet(key);
            return v;
        }

        public string GetString(string key)
        {
            var redisLite = GetLite();
            byte[] v = redisLite.StringGet(key);
            return v.FromUtf8Bytes();
        }

        public async Task<byte[][]> GetAllAsync(string[] keys)
        {
            var redisLite = GetLite();
            return (await redisLite.StringGetAsync(keys.Select(k => (RedisKey) k).ToArray())).Select(rv => (byte[]) rv)
                .ToArray();
        }

        private IDatabase GetLite()
        {
            object stateAsync = new object();
            return redis.GetDatabase(0, stateAsync);
        }
    }
}