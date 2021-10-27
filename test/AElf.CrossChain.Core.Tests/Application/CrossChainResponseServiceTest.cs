using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AElf.CrossChain.Application
{
    public class CrossChainResponseServiceTest : CrossChainCommunicationTestBase
    {
        private readonly ICrossChainResponseService _chainResponseService;
        private readonly CrossChainCommunicationTestHelper _crossChainCommunicationTestHelper;

        public CrossChainResponseServiceTest()
        {
            _chainResponseService = GetRequiredService<ICrossChainResponseService>();
            _crossChainCommunicationTestHelper = GetRequiredService<CrossChainCommunicationTestHelper>();
        }

        [Fact]
        public async Task ResponseSideChainBlockData_Test()
        {
            var res = await _chainResponseService.ResponseSideChainBlockDataAsync(2);
            Assert.True(res.Height == 2);
        }

        [Fact]
        public async Task ResponseParentChainBlockData_WithoutBlock_Test()
        {
            var chainId = ChainHelper.GetChainId(1);
            var height = 5;
            var res = await _chainResponseService.ResponseParentChainBlockDataAsync(height, chainId);
            Assert.Null(res);
        }
        
        [Fact]
        public async Task ResponseParentChainBlockData_WithMerklePath_Test()
        {
            var chainId = 123;
            var height = 3;
            var res = await _chainResponseService.ResponseParentChainBlockDataAsync(height, chainId);
            res.ShouldNotBeNull();

            var indexedSideChainBlockData = _crossChainCommunicationTestHelper.IndexedSideChainBlockData;

            var computedRoot = res.IndexedMerklePath[indexedSideChainBlockData.SideChainBlockDataList[0].Height]
                .ComputeRootWithLeafNode(indexedSideChainBlockData.SideChainBlockDataList[0]
                    .TransactionStatusMerkleTreeRoot);
            
            computedRoot.ShouldBe(res.CrossChainExtraData.TransactionStatusMerkleTreeRoot);
        }
        
        [Fact]
        public async Task ResponseParentChainBlockData_Test()
        {
            var chainId = ChainHelper.GetChainId(1);
            var height = 3;
            var res = await _chainResponseService.ResponseParentChainBlockDataAsync(height, chainId);
            Assert.True(res.Height == 3);
        }
    }
}