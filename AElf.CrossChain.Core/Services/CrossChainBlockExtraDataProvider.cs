using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;

namespace AElf.CrossChain
{
    public class CrossChainBlockExtraDataProvider : IBlockExtraDataProvider
    {
        private readonly ICrossChainContractReader _crossChainContractReader;

        public CrossChainBlockExtraDataProvider(ICrossChainContractReader crossChainContractReader)
        {
            _crossChainContractReader = crossChainContractReader;
        }

        public async Task FillExtraDataAsync(Block block)
        {
            var indexedCrossChainBlockData =
                await _crossChainContractReader.GetCrossChainBlockDataAsync(block.GetHash(), block.Height);
            
            var txRootHashList = indexedCrossChainBlockData.ParentChainBlockData.Select(pcb => pcb.Root.SideChainTransactionsRoot).ToList();
            var calculatedSideChainTransactionsRoot = new BinaryMerkleTree().AddNodes(txRootHashList).ComputeRootHash();
            
            block.Header.BlockExtraData.SideChainTransactionsRoot = calculatedSideChainTransactionsRoot;
        }
    }
}