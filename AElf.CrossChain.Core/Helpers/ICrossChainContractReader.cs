using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Types;

namespace AElf.CrossChain
{
    public interface ICrossChainContractReader
    {
        Task<MerklePath> GetTxRootMerklePathInParentChainAsync(ulong blockHeight);
        Task<ParentChainBlockData> GetBoundParentChainBlockInfoAsync(ulong height);
        Task<ulong> GetBoundParentChainHeightAsync(ulong localChainHeight);
        Task<ulong> GetParentChainCurrentHeightAsync(int chainId, Hash previousBlockHash,
            ulong preBlockHeight);
        Task<ulong> GetSideChainCurrentHeightAsync(int chainId, int sideChainId, Hash previousBlockHash,
            ulong preBlockHeight);

        Task<int> GetParentChainIdAsync(int chainId, Hash previousBlockHash, ulong preBlockHeight);
        Task<Dictionary<int, ulong>> GetSideChainIdAndHeightAsync(int chainId, Hash previousBlockHash,
            ulong preBlockHeight);
        Task<IndexedSideChainBlockDataResult> GetIndexedSideChainBlockInfoResultAsync(ulong height);
        Task<Dictionary<int, ulong>> GetAllChainsIdAndHeightAsync(int chainId, Hash blockHash, ulong blockHeight);
    }
    
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
            return await _crossChainReadOnlyTransactionExecutor.ReadByTransactionAsync<ulong>(chainId,
                crossChainContractMethodAddress, CrossChainConsts.GetParentChainHeightMethodName, previousBlockHash,
                preBlockHeight);
        }

        public async Task<ulong> GetSideChainCurrentHeightAsync(int chainId, int sideChainId, Hash previousBlockHash,
            ulong preBlockHeight)
        {
            var crossChainContractMethodAddress = ContractHelpers.GetCrossChainContractAddress(chainId);
            return await _crossChainReadOnlyTransactionExecutor.ReadByTransactionAsync<ulong>(chainId,
                crossChainContractMethodAddress, CrossChainConsts.GetSideChainHeightMthodName, previousBlockHash,
                preBlockHeight, ChainHelpers.ConvertChainIdToBase58(sideChainId));
        }

        public async Task<int> GetParentChainIdAsync(int chainId, Hash previousBlockHash, ulong preBlockHeight)
        {
            var crossChainContractMethodAddress = ContractHelpers.GetCrossChainContractAddress(chainId);
            return await _crossChainReadOnlyTransactionExecutor.ReadByTransactionAsync<int>(chainId,
                crossChainContractMethodAddress, CrossChainConsts.GetParentChainId, previousBlockHash, preBlockHeight);
        }

        public async Task<Dictionary<int, ulong>> GetSideChainIdAndHeightAsync(int chainId, Hash previousBlockHash, ulong preBlockHeight)
        {
            var crossChainContractMethodAddress = ContractHelpers.GetCrossChainContractAddress(chainId);
            var dict = await _crossChainReadOnlyTransactionExecutor.ReadByTransactionAsync<SideChainIdAndHeightDict>(
                chainId, crossChainContractMethodAddress, CrossChainConsts.GetSideChainIdAndHeight, previousBlockHash,
                preBlockHeight);
            return new Dictionary<int, ulong>(dict.IdHeighDict);
        }

        public Task<IndexedSideChainBlockDataResult> GetIndexedSideChainBlockInfoResultAsync(ulong height)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Dictionary<int, ulong>> GetAllChainsIdAndHeightAsync(int chainId, Hash previousBlockHash, ulong preBlockHeight)
        {
            var crossChainContractMethodAddress = ContractHelpers.GetCrossChainContractAddress(chainId);
            var dict = await _crossChainReadOnlyTransactionExecutor.ReadByTransactionAsync<SideChainIdAndHeightDict>(
                chainId, crossChainContractMethodAddress, CrossChainConsts.GetAllChainsIdAndHeight, previousBlockHash,
                preBlockHeight);
            return new Dictionary<int, ulong>(dict.IdHeighDict);
        }
    }
}
