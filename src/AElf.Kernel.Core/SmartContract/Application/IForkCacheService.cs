using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IForkCacheService
    {
        void RemoveByBlockHash(List<Hash> blockHashes);

        void SetIrreversible(List<Hash> blockHashes);
        
        void SetIrreversible(Hash blockHash);
    }
}