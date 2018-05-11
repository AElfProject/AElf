using System;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IKeyValueDatabase
    {
        Task<byte[]> GetAsync(Hash key,Type type);
        Task SetAsync(Hash key, byte[] bytes);
    }
}