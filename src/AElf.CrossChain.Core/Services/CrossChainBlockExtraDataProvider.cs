using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain
{
    public class CrossChainBlockExtraDataProvider : IBlockExtraDataProvider
    {
        private readonly ICrossChainService _crossChainService;

        public ILogger<CrossChainBlockExtraDataProvider> Logger { get; set; }

        public CrossChainBlockExtraDataProvider(ICrossChainService crossChainService)
        {
            _crossChainService = crossChainService;
        }

        public async Task<ByteString> GetExtraDataForFillingBlockHeaderAsync(BlockHeader blockHeader)
        {
            if (blockHeader.Height == KernelConstants.GenesisBlockHeight)
                return ByteString.Empty;

            //Logger.LogTrace($"Get new cross chain data with hash {blockHeader.PreviousBlockHash}, height {blockHeader.Height - 1}");

            var newCrossChainBlockData =
                await _crossChainService.GetNewCrossChainBlockDataAsync(blockHeader.PreviousBlockHash,
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