using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.CrossChain;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;

namespace AElf.CrossChain
{
    internal class CrossChainValidationProvider : IBlockValidationProvider
    {
        private readonly ICrossChainDataProvider _crossChainDataProvider;
        private readonly IBlockExtraDataService _blockExtraDataService;

        public CrossChainValidationProvider(ICrossChainDataProvider crossChainDataProvider, IBlockExtraDataService blockExtraDataService)
        {
            _crossChainDataProvider = crossChainDataProvider;
            _blockExtraDataService = blockExtraDataService;
        }

        public Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block)
        {
            // nothing to validate before execution for cross chain
            return Task.FromResult(true);
        }

        public async Task<bool> ValidateBeforeAttachAsync(IBlock block)
        {
            return true;
        }

        public async Task<bool> ValidateBlockAfterExecuteAsync(IBlock block)
        {
            if (block.Height == Constants.GenesisBlockHeight)
                return true;
            
            var message =
                await _crossChainDataProvider.GetIndexedCrossChainBlockDataAsync(block.Header.GetHash(), block.Height);
            var indexedCrossChainBlockData = CrossChainBlockData.Parser.ParseFrom(message.ToByteString());
            var extraData = ExtractCrossChainExtraData(block.Header);
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
            var res = await _crossChainDataProvider.ValidateSideChainBlockDataAsync(
                       crossChainBlockData.SideChainBlockData.ToList(), block.Header.PreviousBlockHash, block.Height - 1) &&
                   await _crossChainDataProvider.ValidateParentChainBlockDataAsync(
                       crossChainBlockData.ParentChainBlockData.ToList(), block.Header.PreviousBlockHash, block.Height - 1);
            return res;
        }

        private CrossChainExtraData ExtractCrossChainExtraData(BlockHeader header)
        {
            var bytes = _blockExtraDataService.GetExtraDataFromBlockHeader("CrossChain", header);
            return bytes == ByteString.Empty || bytes == null ? null : CrossChainExtraData.Parser.ParseFrom(bytes);
        }

    }
}