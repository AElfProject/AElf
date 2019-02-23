using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Types;

namespace AElf.CrossChain
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

        public async Task<ulong> GetParentChainCurrentHeightAsync(int chainId, Hash previousBlockHash, ulong preBlockHeight)
        {
            var crossChainContractMethodAddress = ContractHelpers.GetCrossChainContractAddress(chainId);
            return await _crossChainReadOnlyTransactionExecutor.ReadByTransactionAsync<ulong>(chainId, crossChainContractMethodAddress, 
                CrossChainConsts.GetParentChainHeightMethodName, previousBlockHash, preBlockHeight: preBlockHeight);
        }

        public async Task<ulong> GetSideChainCurrentHeightAsync(int chainId, int sideChainId, Hash previousBlockHash,
            ulong preBlockHeight)
        {
            var crossChainContractMethodAddress = ContractHelpers.GetCrossChainContractAddress(chainId);
            return await _crossChainReadOnlyTransactionExecutor.ReadByTransactionAsync<ulong>(chainId, crossChainContractMethodAddress, 
                CrossChainConsts.GetSideChainHeightMthodName, previousBlockHash, preBlockHeight, ChainHelpers.ConvertChainIdToBase58(sideChainId));
        }

        public Task<int> GetParentChainIdAsync(int chainId, Hash previousBlockHash, ulong preBlockHeight)
        {
            throw new System.NotImplementedException();
        }

        public Task<Dictionary<int, ulong>> GetSideChainIdAndHeightAsync(int chainId, Hash previousBlockHash, ulong preBlockHeight)
        {
            throw new System.NotImplementedException();
        }

        public Task<IndexedSideChainBlockDataResult> GetIndexedSideChainBlockInfoResultAsync(ulong height)
        {
            throw new System.NotImplementedException();
        }
    }
}