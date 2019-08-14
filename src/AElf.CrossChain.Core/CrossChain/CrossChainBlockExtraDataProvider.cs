using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace AElf.CrossChain
{
    internal class CrossChainBlockExtraDataProvider : IBlockExtraDataProvider
    {
        private readonly ICrossChainIndexingDataService _crossChainIndexingDataService;

        public ILogger<CrossChainBlockExtraDataProvider> Logger { get; set; }

        public CrossChainBlockExtraDataProvider(ICrossChainIndexingDataService crossChainIndexingDataService)
        {
            _crossChainIndexingDataService = crossChainIndexingDataService;
        }

        public async Task<ByteString> GetExtraDataForFillingBlockHeaderAsync(BlockHeader blockHeader)
        {
            if (blockHeader.Height == Constants.GenesisBlockHeight)
                return ByteString.Empty;

            //Logger.LogTrace($"Get new cross chain data with hash {blockHeader.PreviousBlockHash}, height {blockHeader.Height - 1}");

            var newCrossChainBlockData =
                await _crossChainIndexingDataService.GetCrossChainBlockDataForNextMiningAsync(blockHeader.PreviousBlockHash,
                    blockHeader.Height - 1);
            if (newCrossChainBlockData == null || newCrossChainBlockData.SideChainBlockData.Count == 0)
                return ByteString.Empty;
            
            var txRootHashList = newCrossChainBlockData.SideChainBlockData.Select(scb => scb.TransactionMerkleTreeRoot).ToList();
            var calculatedSideChainTransactionsRoot = BinaryMerkleTree.FromLeafNodes(txRootHashList).Root;

            return new CrossChainExtraData {SideChainTransactionsRoot = calculatedSideChainTransactionsRoot}
                .ToByteString();
        }
    }
}