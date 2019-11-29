using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IForkCacheService
    {
        void MergeAndCleanForkCache(Hash irreversibleBlockHash, long irreversibleBlockHeight);
    }
}