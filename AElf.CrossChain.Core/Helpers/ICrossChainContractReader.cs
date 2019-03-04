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
        Task<MerklePath> GetTxRootMerklePathInParentChainAsync(ulong blockHeight);
        Task<ParentChainBlockData> GetBoundParentChainBlockInfoAsync(ulong height);
        Task<ulong> GetBoundParentChainHeightAsync(ulong localChainHeight);

        Task<ulong> GetParentChainCurrentHeightAsync(Hash previousBlockHash,
            ulong preBlockHeight);

        Task<ulong> GetSideChainCurrentHeightAsync(int sideChainId, Hash previousBlockHash,
            ulong preBlockHeight);

        Task<int> GetParentChainIdAsync(Hash previousBlockHash, ulong preBlockHeight);

        Task<Dictionary<int, ulong>> GetSideChainIdAndHeightAsync(Hash previousBlockHash,
            ulong preBlockHeight);

        Task<IndexedSideChainBlockDataResult> GetIndexedSideChainBlockInfoResultAsync(ulong height);
        Task<Dictionary<int, ulong>> GetAllChainsIdAndHeightAsync(Hash blockHash, ulong blockHeight);

        Task<CrossChainBlockData> GetCrossChainBlockDataAsync(Hash previousBlockHash, ulong previousBlockHeight);
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

        public async Task<ulong> GetParentChainCurrentHeightAsync(Hash previousBlockHash, ulong preBlockHeight)
        {
            return await _crossChainReadOnlyTransactionExecutor.ReadByTransactionAsync<ulong>(
                CrossChainContractMethodAddress, CrossChainConsts.GetParentChainHeightMethodName, previousBlockHash,
                preBlockHeight);
        }

        public async Task<ulong> GetSideChainCurrentHeightAsync(int sideChainId, Hash previousBlockHash,
            ulong preBlockHeight)
        {
            return await _crossChainReadOnlyTransactionExecutor.ReadByTransactionAsync<ulong>(
                CrossChainContractMethodAddress, CrossChainConsts.GetSideChainHeightMthodName, previousBlockHash,
                preBlockHeight, ChainHelpers.ConvertChainIdToBase58(sideChainId));
        }

        public async Task<int> GetParentChainIdAsync(Hash previousBlockHash, ulong preBlockHeight)
        {
            return await _crossChainReadOnlyTransactionExecutor.ReadByTransactionAsync<int>(
                CrossChainContractMethodAddress, CrossChainConsts.GetParentChainId, previousBlockHash, preBlockHeight);
        }

        public async Task<Dictionary<int, ulong>> GetSideChainIdAndHeightAsync(Hash previousBlockHash,
            ulong preBlockHeight)
        {
            var dict = await _crossChainReadOnlyTransactionExecutor.ReadByTransactionAsync<SideChainIdAndHeightDict>(
                CrossChainContractMethodAddress, CrossChainConsts.GetSideChainIdAndHeight, previousBlockHash,
                preBlockHeight);
            return new Dictionary<int, ulong>(dict.IdHeighDict);
        }

        public Task<IndexedSideChainBlockDataResult> GetIndexedSideChainBlockInfoResultAsync(ulong height)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Dictionary<int, ulong>> GetAllChainsIdAndHeightAsync(Hash previousBlockHash,
            ulong preBlockHeight)
        {
            var dict = await _crossChainReadOnlyTransactionExecutor.ReadByTransactionAsync<SideChainIdAndHeightDict>(
                CrossChainContractMethodAddress, CrossChainConsts.GetAllChainsIdAndHeight, previousBlockHash,
                preBlockHeight);
            return dict == null ? null : new Dictionary<int, ulong>(dict.IdHeighDict);
        }

        public async Task<CrossChainBlockData> GetCrossChainBlockDataAsync(Hash previousBlockHash, ulong previousBlockHeight)
        {
            var indexedCrossChainBlockData = await _crossChainReadOnlyTransactionExecutor.ReadByTransactionAsync<CrossChainBlockData>(
                CrossChainContractMethodAddress, CrossChainConsts.GetIndexedCrossChainBlockDataByHeight, previousBlockHash,
                previousBlockHeight);
            return indexedCrossChainBlockData;
        }

        Address CrossChainContractMethodAddress => ContractHelpers.GetCrossChainContractAddress(_chainManager.GetChainId());

    }
}