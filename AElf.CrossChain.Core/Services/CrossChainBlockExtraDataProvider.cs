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

        public CrossChainBlockExtraDataProvider(ICrossChainService crossChainService)
        {
            _crossChainService = crossChainService;
        }

        public async Task<ByteString> GetExtraDataForFillingBlockHeaderAsync(BlockHeader blockHeader)
        {
            var indexedCrossChainBlockData =
                await _crossChainService.GetIndexedCrossChainBlockDataAsync(blockHeader.GetHash(), blockHeader.Height);
            if (indexedCrossChainBlockData == null)
                return null;
            var txRootHashList = indexedCrossChainBlockData.SideChainBlockData.Select(scb => scb.TransactionMKRoot);
            var calculatedSideChainTransactionsRoot = new BinaryMerkleTree().AddNodes(txRootHashList).ComputeRootHash();
            
            return calculatedSideChainTransactionsRoot.ToByteString();
        }
    }
}