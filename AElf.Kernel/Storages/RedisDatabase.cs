using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf.Database;
using ServiceStack;
using ServiceStack.Text;

namespace AElf.Kernel.Storages
{
    public class RedisDatabase : IKeyValueDatabase
    {
        public async Task<byte[]> GetAsync(Hash key, Type type)
        {
            var bytes = await RedisHelper.GetAsync(key.Value.ToBase64());
            return bytes;
        }

        public async Task SetAsync(Hash key, byte[] bytes)
        {
            await RedisHelper.SetAsync(key.Value.ToBase64(), bytes);
        }
    }
}