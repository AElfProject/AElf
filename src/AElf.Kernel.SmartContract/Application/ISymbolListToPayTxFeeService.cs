using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISymbolListToPayTxFeeService
    {
        Task<List<AvailableTokenInfoInCache>> GetExtraAcceptedTokensInfoAsync(IChainContext chainContext);
        void SetExtraAcceptedTokenInfoToForkCache(BlockIndex index, List<AvailableTokenInfoInCache> tokenInfos);
        void RemoveFromForkCacheByBlockIndex(List<BlockIndex> blockIndexes);
        void SyncCache(List<BlockIndex> blockIndexes);
    }
}