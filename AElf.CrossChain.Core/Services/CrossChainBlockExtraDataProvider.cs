using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain
{
    public class CrossChainBlockExtraDataProvider : IBlockExtraDataProvider
    {
        private readonly ICrossChainService _crossChainService;
        private readonly IBlockExtraDataExtractor _blockExtraDataExtractor;

        public CrossChainBlockExtraDataProvider(ICrossChainService crossChainService, 
            IBlockExtraDataExtractor blockExtraDataExtractor)
        {
            _crossChainService = crossChainService;
            _blockExtraDataExtractor = blockExtraDataExtractor;
        }

        public async Task<ByteString> GetExtraDataForFillingBlockHeaderAsync(BlockHeader blockHeader)
        {
            if (blockHeader.Height == CrossChainConsts.GenesisBlockHeight)
                return ByteString.Empty;

            var newCrossChainBlockData =
                await _crossChainService.GetNewCrossChainBlockDataAsync(blockHeader.PreviousBlockHash,
                    blockHeader.Height - 1);
            if (newCrossChainBlockData.SideChainBlockData.Count == 0)
                return ByteString.Empty;
            
            var txRootHashList = newCrossChainBlockData.SideChainBlockData.Select(scb => scb.TransactionMKRoot);
            var calculatedSideChainTransactionsRoot = new BinaryMerkleTree().AddNodes(txRootHashList).ComputeRootHash();

            return new CrossChainExtraData {SideChainTransactionsRoot = calculatedSideChainTransactionsRoot}
                .ToByteString();
        }
    }
}