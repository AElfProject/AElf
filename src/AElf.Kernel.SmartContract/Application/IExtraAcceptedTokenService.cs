using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IExtraAcceptedTokenService
    {
        Task<Dictionary<string, Tuple<int, int>>> GetExtraAcceptedTokensInfoAsync(IChainContext chainContext);
        void SetExtraAcceptedTokenInfoToForkCache(BlockIndex index, Dictionary<string, Tuple<int, int>> tokenInfos);
        void RemoveFromForkCacheByBlockIndex(List<BlockIndex> blockIndexes);
        void SyncCache(List<BlockIndex> blockIndexes);
    }
}