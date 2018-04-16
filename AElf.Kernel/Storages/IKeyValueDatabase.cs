using System;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IKeyValueDatabase : ICloneable
    {
        Task<object> GetAsync(Hash key,Type type);
        Task SetAsync(Hash key, object bytes);
    }
}