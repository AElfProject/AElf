using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IForkCacheHandler
    {
        void RemoveForkCache(List<BlockIndex> blockIndexes);
        
        void SetIrreversedCache(List<BlockIndex> blockIndexes);
    }
}