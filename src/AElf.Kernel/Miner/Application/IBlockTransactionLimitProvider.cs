using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.Miner.Application
{
    public interface IBlockTransactionLimitProvider
    {
        Task InitAsync();
        int GetLimit(IChainContext chainContext);
        void SetLimit(int limit,BlockIndex blockIndex);
        void RemoveForkCache(List<BlockIndex> blockIndexes);
        void SetIrreversedCache(List<BlockIndex> blockIndexes);
    }
}