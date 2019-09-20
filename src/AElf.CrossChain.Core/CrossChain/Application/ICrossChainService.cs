using System.Collections.Generic;
using System.Threading.Tasks;
using Acs7;
using AElf.Kernel;
using AElf.Types;

namespace AElf.CrossChain
{
    public interface ICrossChainService
    {
        Task FinishInitialSyncAsync();

        Dictionary<int, long> GetNeededChainIdAndHeightPairs();
        
        Task<Block> GetNonIndexedBlockAsync(long height);
        
        Task<ChainInitializationData> GetChainInitializationDataAsync(int chainId);
        Task UpdateWithLib(Hash blockHash, long blockHeight);
    }
}