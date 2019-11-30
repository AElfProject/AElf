using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IForkCacheHandler
    {
        Task RemoveForkCacheAsync(List<BlockIndex> blockIndexes);
        
        Task SetIrreversedCacheAsync(List<BlockIndex> blockIndexes);
    }
}