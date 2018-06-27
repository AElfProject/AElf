using System;
using System.Threading.Tasks;
using AElf.Database.Config;
using AElf.Database.SsdbClient;

namespace AElf.Database
{
    public class SsdbDatabase : IKeyValueDatabase
    {
        private readonly Client _client;

        public SsdbDatabase() : this(new DatabaseConfig())
        {
        }

        public SsdbDatabase(IDatabaseConfig config)
        {
            _client = new Client(config.Host, config.Port);
            _client.Connect();
        }

        public async Task<byte[]> GetAsync(string key, Type type)
        {
            return await Task.FromResult(Get(key));
        }

        public async Task SetAsync(string key, byte[] bytes)
        {
            await Task.FromResult(Set(key, bytes));
        }

        private byte[] Get(string key)
        {
            var ret = _client.Get(key, out byte[] result);
            return ret ? result : null;
        }

        private bool Set(string key, byte[] bytes)
        {
            _client.Set(key, bytes);
            return true;
        }

        public bool IsConnected()
        {
            try
            {
                _client.Set("test", "test");
                return true;
            }
            catch (Exception exception)
            {
                return false;
            }
        }
    }
}