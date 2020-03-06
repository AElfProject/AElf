using System.Threading.Tasks;
using AElf.Types;

namespace AElf.CrossChain.Application
{
    public interface ICrossChainService
    {
        Task FinishInitialSyncAsync();

        // Dictionary<int, long> GetNeededChainIdAndHeightPairs();
        //
        // Task<Block> GetNonIndexedBlockAsync(long height);
        //
        // Task<ChainInitializationData> GetChainInitializationDataAsync(int chainId);
        Task UpdateCrossChainDataWithLibAsync(Hash blockHash, long blockHeight);
    }
}