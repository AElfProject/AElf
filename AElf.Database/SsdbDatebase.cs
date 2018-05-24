using System;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using AElf.Database.Client;

namespace AElf.Database
{
    public class SsdbDatebase:IKeyValueDatabase
    {
        private readonly SsdbClient _client;
        
        public SsdbDatebase()
        {
            _client = new SsdbClient("127.0.0.1", 8888);;
        }

        public SsdbDatebase(DatabaseConfig config)
        {
            _client = new SsdbClient(config.IpAddress, config.Port);
        }
        
        public async Task<byte[]> GetAsync(string key, Type type)
        {
            return await Task.FromResult(_client.request("get", key)[1]);
        }

        public async Task SetAsync(string key, byte[] bytes)
        {
            var keyBytes = ConvertToBytes(key);
            await Task.FromResult(_client.request("set",keyBytes,bytes));
        }

        public bool IsConnected()
        {
            try
            {
                _client.set("test", "test");
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        private byte[] ConvertToBytes(string s)	{
            return Encoding.Default.GetBytes(s);
        }
    }
}