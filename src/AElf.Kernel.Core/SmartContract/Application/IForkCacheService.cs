using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IForkCacheService
    {
        void SetIrreversible(Hash blockHash);

        void CleanCache(Hash irreversibleBlockHash, long irreversibleBlockHeight);
    }
}