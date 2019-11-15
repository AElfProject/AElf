using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IForkCacheHandler
    {
        void RemoveForkCache(List<Hash> blockHashes);
        
        void SetIrreversedCache(List<Hash> blockHashes);
        
        void SetIrreversedCache(Hash blockHash);
    }
}