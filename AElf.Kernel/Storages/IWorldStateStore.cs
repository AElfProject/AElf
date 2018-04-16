using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IWorldStateStore
    {
        Task SetData(Hash pointerHash, byte[] data);
        Task<byte[]> GetData(Hash pointerHash);
        Task InsertWorldState(Hash chainId, Hash blockHash, IChangesStore changesStore);
        Task<WorldState> GetWorldState(Hash chainId, Hash blockHash);
    }
}