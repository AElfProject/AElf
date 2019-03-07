using System.Linq;
using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain
{
    public class CrossChainValidationProvider : IBlockValidationProvider, ITransientDependency
    {
        private readonly ICrossChainService _crossChainService;
        private readonly IBlockExtraDataService _blockExtraDataService;

        public CrossChainValidationProvider(ICrossChainService crossChainService, IBlockExtraDataService blockExtraDataService)
        {
            _crossChainService = crossChainService;
            _blockExtraDataService = blockExtraDataService;
        }

        public Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block)
        {
            // nothing to validate before execution for cross chain
            return Task.FromResult(true);
        }

        public async Task<bool> ValidateBlockAfterExecuteAsync(IBlock block)
        {
            var indexedCrossChainBlockData =
                await _crossChainService.GetIndexedCrossChainBlockDataAsync(block.GetHash(), block.Height);
            if (indexedCrossChainBlockData == null)
                return true;
            var sideChainTransactionRootInExtraData = Hash.LoadByteArray(_blockExtraDataService
                .GetExtraDataFromBlockHeader("CrossChain", block.Header).ToByteArray());
            bool res = await ValidateCrossChainBlockDataAsync(indexedCrossChainBlockData, sideChainTransactionRootInExtraData,
                block.GetHash(), block.Height);
            if(!res)
                throw new ValidateNextTimeBlockValidationException("Cross chain validation failed after execution.");
            return true;
        }

        private async Task<bool> ValidateCrossChainBlockDataAsync(CrossChainBlockData crossChainBlockData, Hash sideChainTransactionsRoot,
            Hash preBlockHash, long preBlockHeight)
        {
            var txRootHashList = crossChainBlockData.ParentChainBlockData.Select(pcb => pcb.Root.SideChainTransactionsRoot).ToList();
            var calculatedSideChainTransactionsRoot = new BinaryMerkleTree().AddNodes(txRootHashList).ComputeRootHash();
            
            // first check equality with the root in header
            if (sideChainTransactionsRoot != null && !calculatedSideChainTransactionsRoot.Equals(sideChainTransactionsRoot))
                return false;
            
            return await _crossChainService.ValidateSideChainBlockDataAsync(
                       crossChainBlockData.SideChainBlockData, preBlockHash, preBlockHeight) &&
                   await _crossChainService.ValidateParentChainBlockDataAsync(
                       crossChainBlockData.ParentChainBlockData, preBlockHash, preBlockHeight);
        }
    }
}