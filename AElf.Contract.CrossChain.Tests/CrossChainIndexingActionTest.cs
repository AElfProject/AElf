using System.Threading.Tasks;
using AElf.Common;
using AElf.CrossChain;
using Xunit;

namespace AElf.Contract.CrossChain.Tests
{
    public class CrossChainIndexingActionTest : CrossChainContractTestBase
    {
        [Fact]
        public async Task RecordCrossChainData()
        {
            int parentChainId = 123;
            var sideChainId = await InitAndCreateSideChain(parentChainId);
            var fakeSideChainBlockHash = Hash.FromString("sideChainBlockHash");
            var fakeTxMerkleTreeRoot = Hash.FromString("txMerkleTreeRoot");
            var sideChainBlockData = new SideChainBlockData
            {
                BlockHeaderHash = fakeSideChainBlockHash,
                SideChainHeight = 1,
                SideChainId = sideChainId,
                TransactionMKRoot = fakeTxMerkleTreeRoot
            };
            var parentChainBlockData = new ParentChainBlockData
            {
                Root = new ParentChainBlockRootInfo
                {
                    ParentChainId = parentChainId,
                    ParentChainHeight = 1
                }
            };
            var crossChainBlockData = new CrossChainBlockData
            {
                ParentChainBlockData = {parentChainBlockData},
                SideChainBlockData = { sideChainBlockData}
            };
            
        }
    }
}