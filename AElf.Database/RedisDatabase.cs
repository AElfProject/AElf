using System;
using System.Threading.Tasks;
using AElf.Database.Config;
using StackExchange.Redis;

namespace AElf.Database
{
    public class RedisDatabase : IKeyValueDatabase
    {
        private readonly ConfigurationOptions _options;

        public RedisDatabase()
        : this(new DatabaseConfig {Host = "127.0.0.1", Port = 8888})
        {
        }

        public RedisDatabase(IDatabaseConfig config)
        {
            _options = new ConfigurationOptions
            {
                EndPoints = {{config.Host, config.Port}},
                CommandMap = CommandMap.Default
            };
        }

        public async Task<byte[]> GetAsync(string key, Type type)
        {
            using (var conn = ConnectionMultiplexer.Connect(_options))
            {
                var db = conn.GetDatabase(0);
                return await db.StringGetAsync(key);
            }
        }

        public async Task SetAsync(string key, ISerializable data)
        {
            using (var conn = ConnectionMultiplexer.Connect(_options))
            {
                var db = conn.GetDatabase(0);
                await db.StringSetAsync(key, data.Serialize());
            }
        }

        public bool IsConnected()
        {
            try
            {
                using (var conn = ConnectionMultiplexer.Connect(_options))
                {
                    var db = conn.GetDatabase(0);
                    db.Ping();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}