using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.SmartContract.Application
{
    //TODO: smart contract should not know fee, fee is just a plugin
    public interface ISymbolListToPayTxFeeService
    {
        Task<List<AvailableTokenInfoInCache>> GetExtraAcceptedTokensInfoAsync(IChainContext chainContext);
        
        //TODO: don't care about fork
        void SetExtraAcceptedTokenInfoToForkCache(BlockIndex index, List<AvailableTokenInfoInCache> tokenInfos);
        void RemoveFromForkCacheByBlockIndex(List<BlockIndex> blockIndexes);
        void SyncCache(List<BlockIndex> blockIndexes);
    }
}