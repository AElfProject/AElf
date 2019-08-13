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

        Task RegisterNewChainsAsync(Hash blockHash, long blockHeight);
        
        Task<Block> GetNonIndexedBlockAsync(long height);
        
        Task<ChainInitializationData> GetChainInitializationDataAsync(int chainId);
    }
}