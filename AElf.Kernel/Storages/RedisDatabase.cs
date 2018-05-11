using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf.Database;
using ServiceStack;
using ServiceStack.Text;

namespace AElf.Kernel.Storages
{
/*    public class RedisDatabase : IKeyValueDatabase
    {
        public async Task<byte[]> GetAsync(Hash key, Type type)
        {
            var k = key.Value.ToBase64();
            var bytes = await RedisHelper.GetAsync(k);
            return bytes;
        }

        public async Task SetAsync(Hash key, byte[] bytes)
        {
            var keyStr = key.Value.ToBase64();
            await RedisHelper.SetAsync(keyStr, bytes);
        }
    }*/
}