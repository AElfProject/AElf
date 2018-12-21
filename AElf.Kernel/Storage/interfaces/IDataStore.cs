using System.Threading.Tasks;
using AElf.Common;
using Google.Protobuf;

namespace AElf.Kernel.Storage.Interfaces
{
    public interface IDataStore
    {
        Task InsertAsync<T>(Hash pointerHash, T obj) where T : IMessage;
        Task<T> GetAsync<T>(Hash pointerHash) where T : IMessage, new();
        Task RemoveAsync<T>(Hash txId) where T : IMessage;
    }
}