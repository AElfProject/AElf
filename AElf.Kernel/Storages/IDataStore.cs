using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf;
using AElf.Common;

namespace AElf.Kernel.Storages
{
    public interface IDataStore
    {
        Task InsertAsync<T>(Hash pointerHash, T obj) where T : IMessage;
        Task<T> GetAsync<T>(Hash pointerHash) where T : IMessage, new();
        Task RemoveAsync<T>(Hash txId) where T : IMessage;
    }
}