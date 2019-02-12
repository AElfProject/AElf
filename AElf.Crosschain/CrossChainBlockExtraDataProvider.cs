using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.BlockService;

namespace AElf.Crosschain
{
    public class CrossChainBlockExtraDataProvider : IBlockExtraDataProvider
    {
        private readonly ICrossChainService _crossChainService;

        public CrossChainBlockExtraDataProvider(ICrossChainService crossChainService)
        {
            _crossChainService = crossChainService;
        }

        public async Task FillExtraData(Block block)
        {
            if(block.Header.BlockExtraData == null)
                block.Header.BlockExtraData = new BlockExtraData();
            var sideChainBlockData = await _crossChainService.GetSideChainBlockInfo();
            var sideChainTransactionsRoot = new BinaryMerkleTree()
                .AddNodes(sideChainBlockData.Select(scb => scb.TransactionMKRoot).ToArray()).ComputeRootHash();
            block.Header.BlockExtraData.SideChainTransactionsRoot = sideChainTransactionsRoot;
            block.Body.SideChainBlockData.AddRange(sideChainBlockData);
            
            var parentChainBlockData = await _crossChainService.GetParentChainBlockInfo();
            block.Body.ParentChainBlockData.AddRange(parentChainBlockData);
        }
    }
}