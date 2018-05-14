using System;
using System.Threading.Tasks;

namespace AElf.Database
{
    public interface IKeyValueDatabase
    {
        Task<byte[]> GetAsync(string key,Type type);
        Task SetAsync(string key, byte[] bytes);
        bool IsConnected();
    }
}