using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;

namespace AElf.CrossChain
{
    public class CrossChainValidationProvider : IBlockValidationProvider
    {
        private readonly ICrossChainDataProvider _crossChainDataProvider;
        private readonly ICrossChainExtraDataExtractor _crossChainExtraDataExtractor;

        public CrossChainValidationProvider(ICrossChainDataProvider crossChainDataProvider, ICrossChainExtraDataExtractor crossChainExtraDataExtractor)
        {
            _crossChainDataProvider = crossChainDataProvider;
            _crossChainExtraDataExtractor = crossChainExtraDataExtractor;
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
                await _crossChainDataProvider.GetIndexedCrossChainBlockDataAsync(block.Header.GetHash(), block.Height);
            var extraData = _crossChainExtraDataExtractor.ExtractCrossChainData(block.Header);
            if (indexedCrossChainBlockData == null)
            {
                return extraData == null;
            }
            
            var res = await ValidateCrossChainBlockDataAsync(indexedCrossChainBlockData, extraData, block);
            return res;
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
            var res = await _crossChainDataProvider.ValidateSideChainBlockDataAsync(
                       crossChainBlockData.SideChainBlockData.ToList(), block.Header.PreviousBlockHash, block.Height - 1) &&
                   await _crossChainDataProvider.ValidateParentChainBlockDataAsync(
                       crossChainBlockData.ParentChainBlockData.ToList(), block.Header.PreviousBlockHash, block.Height - 1);
            return res;
        }
    }
}