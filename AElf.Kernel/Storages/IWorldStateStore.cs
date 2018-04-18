using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IWorldStateStore
    {
        Task InsertWorldState(Hash chainId, Hash blockHash, IChangesCollection changesCollection);
        Task<WorldState> GetWorldState(Hash chainId, Hash blockHash);
    }
}