using System;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace AElf.Database
{
    public class SsdbDatabase:IKeyValueDatabase
    {
        private readonly string _ipAddress;
        private readonly int _port;
        
        public SsdbDatabase()
        {
            _ipAddress = "127.0.0.1";
            _port = 8888;
        }

        public SsdbDatabase(DatabaseConfig config)
        {
            _ipAddress = config.IpAddress;
            _port = config.Port;
        }
        
        public async Task<byte[]> GetAsync(string key, Type type)
        {
            return await Task.FromResult(Get(key));
        }

        public async Task SetAsync(string key, byte[] bytes)
        {
            await Task.FromResult(Set(key, bytes));
        }

        public bool IsConnected()
        {
            try
            {
                using (var client = new SsdbClient.Client(_ipAddress, _port))
                {
                    return client.Connect();
                }
            }
            catch
            {
                return false;
            }
        }

        private byte[] Get(string key)
        {
            using (var client = new SsdbClient.Client(_ipAddress, _port))
            {
                client.Connect();
                client.Get(key, out byte[] result);
                return result;
            }
        }

        private bool Set(string key, byte[] bytes)
        {
            using (var client = new SsdbClient.Client(_ipAddress, _port))
            {
                client.Connect();
                client.Set(key, bytes);
            }
            return true;
        }
    }
}