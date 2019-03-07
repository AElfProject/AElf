using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Types;

namespace AElf.CrossChain
{
    public interface ICrossChainContractReader
    {
        Task<MerklePath> GetTxRootMerklePathInParentChainAsync(long blockHeight);
        Task<ParentChainBlockData> GetBoundParentChainBlockInfoAsync(long height);
        Task<long> GetBoundParentChainHeightAsync(long localChainHeight);

        Task<long> GetParentChainCurrentHeightAsync(Hash previousBlockHash,
            long preBlockHeight);

        Task<long> GetSideChainCurrentHeightAsync(int sideChainId, Hash previousBlockHash,
            long preBlockHeight);

        Task<int> GetParentChainIdAsync(Hash previousBlockHash, long preBlockHeight);

        Task<Dictionary<int, long>> GetSideChainIdAndHeightAsync(Hash previousBlockHash,
            long preBlockHeight);

        Task<IndexedSideChainBlockDataResult> GetIndexedSideChainBlockInfoResultAsync(long height);
        Task<Dictionary<int, long>> GetAllChainsIdAndHeightAsync(Hash blockHash, long blockHeight);
    }

    public class CrossChainContractReader : ICrossChainContractReader
    {
        private readonly ICrossChainReadOnlyTransactionExecutor _crossChainReadOnlyTransactionExecutor;
        private IChainManager _chainManager;

        public CrossChainContractReader(ICrossChainReadOnlyTransactionExecutor crossChainReadOnlyTransactionExecutor, IChainManager chainManager)
        {
            _crossChainReadOnlyTransactionExecutor = crossChainReadOnlyTransactionExecutor;
            _chainManager = chainManager;
        }

        public Task<MerklePath> GetTxRootMerklePathInParentChainAsync(long blockHeight)
        {
            throw new System.NotImplementedException();
        }

        public Task<ParentChainBlockData> GetBoundParentChainBlockInfoAsync(long height)
        {
            throw new System.NotImplementedException();
        }

        public Task<long> GetBoundParentChainHeightAsync(long localChainHeight)
        {
            throw new System.NotImplementedException();
        }

        public async Task<long> GetParentChainCurrentHeightAsync(Hash previousBlockHash, long preBlockHeight)
        {
            return await _crossChainReadOnlyTransactionExecutor.ReadByTransactionAsync<long>(
                CrossChainContractMethodAddress, CrossChainConsts.GetParentChainHeightMethodName, previousBlockHash,
                preBlockHeight);
        }

        public async Task<long> GetSideChainCurrentHeightAsync(int sideChainId, Hash previousBlockHash,
            long preBlockHeight)
        {
            return await _crossChainReadOnlyTransactionExecutor.ReadByTransactionAsync<long>(
                CrossChainContractMethodAddress, CrossChainConsts.GetSideChainHeightMthodName, previousBlockHash,
                preBlockHeight, ChainHelpers.ConvertChainIdToBase58(sideChainId));
        }

        public async Task<int> GetParentChainIdAsync(Hash previousBlockHash, long preBlockHeight)
        {
            return await _crossChainReadOnlyTransactionExecutor.ReadByTransactionAsync<int>(
                CrossChainContractMethodAddress, CrossChainConsts.GetParentChainId, previousBlockHash, preBlockHeight);
        }

        public async Task<Dictionary<int, long>> GetSideChainIdAndHeightAsync(Hash previousBlockHash,
            long preBlockHeight)
        {
            var dict = await _crossChainReadOnlyTransactionExecutor.ReadByTransactionAsync<SideChainIdAndHeightDict>(
                CrossChainContractMethodAddress, CrossChainConsts.GetSideChainIdAndHeight, previousBlockHash,
                preBlockHeight);
            return new Dictionary<int, long>(dict.IdHeighDict);
        }

        public Task<IndexedSideChainBlockDataResult> GetIndexedSideChainBlockInfoResultAsync(long height)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Dictionary<int, long>> GetAllChainsIdAndHeightAsync(Hash previousBlockHash,
            long preBlockHeight)
        {
            var dict = await _crossChainReadOnlyTransactionExecutor.ReadByTransactionAsync<SideChainIdAndHeightDict>(
                CrossChainContractMethodAddress, CrossChainConsts.GetAllChainsIdAndHeight, previousBlockHash,
                preBlockHeight);
            return new Dictionary<int, long>(dict.IdHeighDict);
        }
        
        Address CrossChainContractMethodAddress => ContractHelpers.GetCrossChainContractAddress(_chainManager.GetChainId());

    }
}