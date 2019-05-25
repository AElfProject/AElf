using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace AElf.CrossChain
{
    internal class CrossChainBlockExtraDataProvider : IBlockExtraDataProvider
    {
        private readonly ICrossChainDataProvider _crossChainDataProvider;

        public ILogger<CrossChainBlockExtraDataProvider> Logger { get; set; }

        public CrossChainBlockExtraDataProvider(ICrossChainDataProvider crossChainDataProvider)
        {
            _crossChainDataProvider = crossChainDataProvider;
        }

        public async Task<ByteString> GetExtraDataForFillingBlockHeaderAsync(BlockHeader blockHeader)
        {
            if (blockHeader.Height == Constants.GenesisBlockHeight)
                return ByteString.Empty;

            //Logger.LogTrace($"Get new cross chain data with hash {blockHeader.PreviousBlockHash}, height {blockHeader.Height - 1}");

            var newCrossChainBlockData =
                await _crossChainDataProvider.GetCrossChainBlockDataForNextMiningAsync(blockHeader.PreviousBlockHash,
                    blockHeader.Height - 1);
            if (newCrossChainBlockData == null || newCrossChainBlockData.SideChainBlockData.Count == 0)
                return ByteString.Empty;
            
            var txRootHashList = newCrossChainBlockData.SideChainBlockData.Select(scb => scb.TransactionMerkleTreeRoot);
            var calculatedSideChainTransactionsRoot = new BinaryMerkleTree().AddNodes(txRootHashList).ComputeRootHash();

            return new CrossChainExtraData {SideChainTransactionsRoot = calculatedSideChainTransactionsRoot}
                .ToByteString();
        }
    }
}