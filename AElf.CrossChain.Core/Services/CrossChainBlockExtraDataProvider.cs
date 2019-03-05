using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;

namespace AElf.CrossChain
{
    public class CrossChainBlockExtraDataProvider : IBlockExtraDataProvider
    {
        private readonly ICrossChainService _crossChainService;

        public CrossChainBlockExtraDataProvider(ICrossChainService crossChainService)
        {
            _crossChainService = crossChainService;
        }

        public async Task FillExtraDataAsync(Block block)
        {
            var indexedCrossChainBlockData =
                await _crossChainService.GetIndexedCrossChainBlockDataAsync(block.GetHash(), block.Height);
            
            var txRootHashList = indexedCrossChainBlockData.ParentChainBlockData.Select(pcb => pcb.Root.SideChainTransactionsRoot).ToList();
            var calculatedSideChainTransactionsRoot = new BinaryMerkleTree().AddNodes(txRootHashList).ComputeRootHash();
            
            block.Header.BlockExtraData.SideChainTransactionsRoot = calculatedSideChainTransactionsRoot;
        }
    }
}