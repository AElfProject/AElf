using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Domain;
using Moq;
using Xunit;

namespace AElf.CrossChain
{
    public class CrossChainServiceTest : CrossChainTestBase
    {
        private readonly ICrossChainService _crossChainService;
        private readonly CrossChainTestHelper _crossChainTestHelper;
        
        public CrossChainServiceTest()
        {
            _crossChainService = GetRequiredService<ICrossChainService>();
            _crossChainTestHelper = GetRequiredService<CrossChainTestHelper>();
        }
        
        [Fact]
        public async Task GetSideChainBlock_WithoutCache()
        {
            var mockSideChainBlockData = new List<SideChainBlockData>();
            var actual = await _crossChainService.GetSideChainBlockDataAsync(Hash.Empty, 1);
            Assert.Equal(mockSideChainBlockData, actual);
        }

        [Fact]
        public async Task GetSideChainBlock_WithoutEnoughCache()
        {
            int stubChainId = 123;
            var mockSideChainBlockData = new List<IBlockInfo>
            {
                new SideChainBlockData
                {
                    SideChainId = stubChainId,
                    SideChainHeight = 1
                }
            };
            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{stubChainId, mockSideChainBlockData}};
            AddFakeCacheData(fakeCache);
            _crossChainTestHelper.AddFakeSideChainIdHeight(stubChainId, 0);
            var actual = await _crossChainService.GetSideChainBlockDataAsync(Hash.Empty, 1);
            Assert.True(actual.Count == 0);
        }

        [Fact]
        public async Task GetParentChainBlock_WithoutCache()
        {
            var mockParentChainBlockData = new List<ParentChainBlockData>();
            int parentChainId = 1;
            _crossChainTestHelper.AddFakeParentChainIdHeight(parentChainId, 1);
            var actual = await _crossChainService.GetParentChainBlockDataAsync(Hash.Empty, 1);
            Assert.Equal(mockParentChainBlockData, actual);
        }

        [Fact]
        public async Task GetParentChainBlock_WithoutEnoughCache()
        {
            int stubChainId = 123;
            var mockParentChainBlockData = new List<IBlockInfo>
            {
                new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = stubChainId,
                        ParentChainHeight = 2
                    }
                }
            };
            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{stubChainId, mockParentChainBlockData}};
            AddFakeCacheData(fakeCache);
            _crossChainTestHelper.AddFakeParentChainIdHeight(stubChainId, 1);

            var res = await _crossChainService.GetParentChainBlockDataAsync(Hash.Empty, 1);
            Assert.True(res.Count == 0);
        }

        [Fact]
        public async Task ValidateSideChainBlock_Success()
        {
            int stubChainId = 123;
            var mockSideChainBlockData = new List<IBlockInfo>
            {
                new SideChainBlockData
                {
                    SideChainId = stubChainId,
                    SideChainHeight = 1
                }
            };
            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{stubChainId, mockSideChainBlockData}};
            AddFakeCacheData(fakeCache);
            _crossChainTestHelper.AddFakeSideChainIdHeight(stubChainId, 0);

            var res = await _crossChainService.ValidateSideChainBlockDataAsync(
                mockSideChainBlockData.Select(scb => (SideChainBlockData) scb).ToList(), Hash.Empty, 1);
            Assert.True(res);
        }

        [Fact]
        public async Task ValidateSideChainBlock_Fail_WrongIndex()
        {
            int stubChainId = 123;
            var mockSideChainBlockData = new List<IBlockInfo>
            {
                new SideChainBlockData
                {
                    SideChainId = stubChainId,
                    SideChainHeight = 1
                }
            };

            var newMockSideChainBlockData = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    SideChainId = stubChainId,
                    SideChainHeight = 2
                }
            };
            _crossChainTestHelper.AddFakeSideChainIdHeight(stubChainId, 1);
            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{stubChainId, mockSideChainBlockData}};
            AddFakeCacheData(fakeCache);
            var res = await _crossChainService.ValidateSideChainBlockDataAsync(newMockSideChainBlockData, Hash.Empty,
                1);
            Assert.False(res);
        }

        [Fact]
        public async Task ValidateParentChainBlock_Success()
        {
            int stubChainId = 123;
            var mockParentChainBlockData = new List<IBlockInfo>
            {
                new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = stubChainId,
                        ParentChainHeight = 1
                    }
                }
            };
            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{stubChainId, mockParentChainBlockData}};
            _crossChainTestHelper.AddFakeParentChainIdHeight(stubChainId, 0);
            AddFakeCacheData(fakeCache);
           
            var res = await _crossChainService.ValidateParentChainBlockDataAsync(
                mockParentChainBlockData.Select(pcb => (ParentChainBlockData) pcb).ToList(), Hash.Empty, 1);
            Assert.True(res);
        }

        [Fact]
        public async Task ValidateParentChainBlock_Fail_WrongIndex()
        {
            int stubChainId = 123;
            var mockParentChainBlockData = new List<IBlockInfo>
            {
                new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = stubChainId,
                        ParentChainHeight = 1
                    }
                }
            };
            var newMockParentChainBlockData = new List<ParentChainBlockData>
            {
                new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ParentChainId = stubChainId,
                        ParentChainHeight = 2
                    }
                }
            };

            var fakeCache = new Dictionary<int, List<IBlockInfo>> {{stubChainId, mockParentChainBlockData}};
            _crossChainTestHelper.AddFakeParentChainIdHeight(stubChainId, 1);
            AddFakeCacheData(fakeCache);
            var res = await _crossChainService.ValidateParentChainBlockDataAsync(newMockParentChainBlockData,
                Hash.Empty, 1);
            Assert.False(res);
        }
        
    }
}