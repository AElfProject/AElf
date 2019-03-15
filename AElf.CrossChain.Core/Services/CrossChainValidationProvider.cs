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
            if (block.Height == CrossChainConsts.GenesisBlockHeight)
                return true;
            
            var indexedCrossChainBlockData =
                await _crossChainService.GetIndexedCrossChainBlockDataAsync(block.Header.PreviousBlockHash, block.Height - 1);
            var extraData = _blockExtraDataExtractor.ExtractCrossChainExtraData(block.Header);
            if (indexedCrossChainBlockData == null)
            {
                return extraData != null;
            }
            
            bool res = await ValidateCrossChainBlockDataAsync(indexedCrossChainBlockData, extraData, block.GetHash(), block.Height);
            if(!res)
                throw new ValidateNextTimeBlockValidationException("Cross chain validation failed after execution.");
            return true;
        }

        private async Task<bool> ValidateCrossChainBlockDataAsync(CrossChainBlockData crossChainBlockData, CrossChainExtraData extraData,
            Hash preBlockHash, long preBlockHeight)
        {
            var txRootHashList = crossChainBlockData.SideChainBlockData.Select(scb => scb.TransactionMKRoot).ToList();
            var calculatedSideChainTransactionsRoot = new BinaryMerkleTree().AddNodes(txRootHashList).ComputeRootHash();
            
            // first check identity with the root in header
            if (calculatedSideChainTransactionsRoot.Equals(Hash.Empty) && extraData == null ||
                !calculatedSideChainTransactionsRoot.Equals(extraData.SideChainTransactionsRoot))
                return false;
            
            // check cache identity
            return await _crossChainService.ValidateSideChainBlockDataAsync(
                       crossChainBlockData.SideChainBlockData.ToList(), preBlockHash, preBlockHeight) &&
                   await _crossChainService.ValidateParentChainBlockDataAsync(
                       crossChainBlockData.ParentChainBlockData.ToList(), preBlockHash, preBlockHeight);
        }
    }
}