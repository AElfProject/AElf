using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf.Database;
using ServiceStack;
using ServiceStack.Text;

namespace AElf.Kernel.Storages
{
    /*public class RedisDatabase : IKeyValueDatabase
    {
        public async Task<byte[]> GetAsync(string key, Type type)
        {
            var bytes = await RedisHelper.GetAsync(key);
            return bytes;
        }

        public async Task SetAsync(string key, byte[] bytes)
        {
            await RedisHelper.SetAsync(key, bytes);
        }
    }*/
}