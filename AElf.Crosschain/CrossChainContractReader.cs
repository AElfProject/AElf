using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Types;

namespace AElf.Crosschain
{
    public class CrossChainContractReader : ICrossChainContractReader
    {
        private readonly ICrossChainReadOnlyTransactionExecutor _crossChainReadOnlyTransactionExecutor;

        public CrossChainContractReader(ICrossChainReadOnlyTransactionExecutor crossChainReadOnlyTransactionExecutor)
        {
            _crossChainReadOnlyTransactionExecutor = crossChainReadOnlyTransactionExecutor;
        }


        public Task<MerklePath> GetTxRootMerklePathInParentChainAsync(ulong blockHeight)
        {
            throw new System.NotImplementedException();
        }

        public Task<ParentChainBlockData> GetBoundParentChainBlockInfoAsync(ulong height)
        {
            throw new System.NotImplementedException();
        }

        public Task<ulong> GetBoundParentChainHeightAsync(ulong localChainHeight)
        {
            throw new System.NotImplementedException();
        }

        public async Task<ulong> GetParentChainCurrentHeightAsync(int chainId, int parentChainId)
        {
            var crossChainContractMethodAddress = ContractHelpers.GetCrossChainContractAddress(chainId);
            return await _crossChainReadOnlyTransactionExecutor.ReadByTransactionAsync<ulong>(chainId, crossChainContractMethodAddress, 
                CrossChainConsts.GetParentChainHeightMethodName);
        }

        public async Task<ulong> GetSideChainCurrentHeightAsync(int chainId, int sideChainId)
        {
            var crossChainContractMethodAddress = ContractHelpers.GetCrossChainContractAddress(chainId);
            return await _crossChainReadOnlyTransactionExecutor.ReadByTransactionAsync<ulong>(chainId, crossChainContractMethodAddress, 
                CrossChainConsts.GetSideChainHeightMthodName, ChainHelpers.ConvertChainIdToBase58(sideChainId));
        }

        public Task<IndexedSideChainBlockDataResult> GetIndexedSideChainBlockInfoResultAsync(ulong height)
        {
            throw new System.NotImplementedException();
        }
    }
}