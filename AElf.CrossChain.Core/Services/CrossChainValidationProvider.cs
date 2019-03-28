using System.Linq;
using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain
{
    public class CrossChainValidationProvider : IBlockValidationProvider
    {
        private readonly ICrossChainService _crossChainService;
        private readonly IBlockExtraDataExtractor _blockExtraDataExtractor;

        public CrossChainValidationProvider(ICrossChainService crossChainService, IBlockExtraDataExtractor blockExtraDataExtractor)
        {
            _crossChainService = crossChainService;
            _blockExtraDataExtractor = blockExtraDataExtractor;
        }

        public Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block)
        {
            // nothing to validate before execution for cross chain
            return Task.FromResult(true);
        }

        public async Task<bool> ValidateBlockAfterExecuteAsync(IBlock block)
        {
            if (block.Height == KernelConstants.GenesisBlockHeight)
                return true;
            
            var indexedCrossChainBlockData =
                await _crossChainService.GetCrossChainBlockDataIndexedInStateAsync(block.Header.GetHash(), block.Height);
            var extraData = _blockExtraDataExtractor.ExtractCrossChainExtraData(block.Header);
            if (indexedCrossChainBlockData == null)
            {
                return extraData == null;
            }
            
            bool res = await ValidateCrossChainBlockDataAsync(indexedCrossChainBlockData, extraData, block);
            if(!res)
                throw new ValidateNextTimeBlockValidationException("Cross chain validation failed after execution.");
            return true;
        }

        private async Task<bool> ValidateCrossChainBlockDataAsync(CrossChainBlockData crossChainBlockData, 
            CrossChainExtraData extraData, IBlock block)
        {
            var txRootHashList = crossChainBlockData.SideChainBlockData.Select(scb => scb.TransactionMerkleTreeRoot).ToList();
            var calculatedSideChainTransactionsRoot = new BinaryMerkleTree().AddNodes(txRootHashList).ComputeRootHash();
            
            // first check identity with the root in header
            if (extraData != null && !calculatedSideChainTransactionsRoot.Equals(extraData.SideChainTransactionsRoot) ||
                extraData == null && !calculatedSideChainTransactionsRoot.Equals(Hash.Empty))
                return false;
            
            // check cache identity
            var res = await _crossChainService.ValidateSideChainBlockDataAsync(
                       crossChainBlockData.SideChainBlockData.ToList(), block.Header.PreviousBlockHash, block.Height - 1) &&
                   await _crossChainService.ValidateParentChainBlockDataAsync(
                       crossChainBlockData.ParentChainBlockData.ToList(), block.Header.PreviousBlockHash, block.Height - 1);
            return res;
        }
    }
}