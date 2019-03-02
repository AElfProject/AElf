using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using Moq;
using Xunit;

namespace AElf.CrossChain
{
    public class CrossChainServiceTest : CrossChainTestBase
    {
        private ICrossChainService CreateNewCrossChainService(Dictionary<int, List<IBlockInfo>> fakeCache,
            Dictionary<int, ulong> sideChainIdHeights, Dictionary<int, ulong> parentCHainIdHeights)
        {
            var fakeMultiChainBlockInfoCacheProvider = CreateFakeMultiChainBlockInfoCacheProvider(fakeCache);
            var fakeConsumer = CreateFakeCrossChainDataConsumer(fakeMultiChainBlockInfoCacheProvider);
            var mockContractReader = CreateFakeCrossChainContractReader(sideChainIdHeights, parentCHainIdHeights);
            var crossChainDataProvider = new CrossChainDataProvider(mockContractReader, fakeConsumer);
            return new CrossChainService(crossChainDataProvider);
        }
        
        [Fact]
        public async Task GetSideChainBlock_WithoutCache()
        {
            int stubChainId = 123;
            var mockSideChainBlockData = new List<SideChainBlockData>();
            var crossChainService = CreateNewCrossChainService(
                new Dictionary<int, List<IBlockInfo>>(), new Dictionary<int, ulong>(), new Dictionary<int, ulong>());
            var actual = await crossChainService.GetSideChainBlockDataAsync(stubChainId, Hash.Default, 1);
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
            var fakeCache = new Dictionary<int, List<IBlockInfo> >{{stubChainId, mockSideChainBlockData}};
            var sideChainIdHeights = new Dictionary<int, ulong>
            {
                {stubChainId, 0}
            };
            var crossChainService = CreateNewCrossChainService(fakeCache, sideChainIdHeights, new Dictionary<int, ulong>());
            var actual = await crossChainService.GetSideChainBlockDataAsync(stubChainId, Hash.Default, 1);
            Assert.True(actual.Count == 0);
        }
        
        [Fact]
        public async Task GetParentChainBlock_WithoutCache()
        {
            int stubChainId = 123;
            var mockParentChainBlockData = new List<ParentChainBlockData>();
            var crossChainService = CreateNewCrossChainService(
                new Dictionary<int, List<IBlockInfo>>(), new Dictionary<int, ulong>(), new Dictionary<int, ulong>());
            var actual = await crossChainService.GetParentChainBlockDataAsync(stubChainId, Hash.Default, 1);
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
                        ParentChainHeight = 1
                    }
                }
            };
            var fakeCache = new Dictionary<int, List<IBlockInfo> >{{stubChainId, mockParentChainBlockData}};
            var parentChainIdHeights = new Dictionary<int, ulong>
            {
                {stubChainId, 0}
            };
            var crossChainService = CreateNewCrossChainService(fakeCache, new Dictionary<int, ulong>(), parentChainIdHeights);
            var res = await crossChainService.GetParentChainBlockDataAsync(stubChainId, Hash.Default, 1);
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
            var fakeCache = new Dictionary<int, List<IBlockInfo> >{{stubChainId, mockSideChainBlockData}};
            var sideChainIdHeights = new Dictionary<int, ulong>
            {
                {stubChainId, 1}
            };
            var crossChainService = CreateNewCrossChainService(fakeCache, sideChainIdHeights, new Dictionary<int, ulong>());
            var res = await crossChainService.ValidateSideChainBlockDataAsync(stubChainId,
                mockSideChainBlockData.Select(scb => (SideChainBlockData) scb).ToList(), Hash.Default, 1);
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
                    SideChainId = stubChainId + 1,
                    SideChainHeight = 2
                }
            }; 
            var sideChainIdHeights = new Dictionary<int, ulong>
            {
                {stubChainId, 1}
            };
            var fakeCache = new Dictionary<int, List<IBlockInfo> >{{stubChainId, mockSideChainBlockData}};
            var crossChainService = CreateNewCrossChainService(fakeCache, sideChainIdHeights, new Dictionary<int, ulong>());
            var res = await crossChainService.ValidateSideChainBlockDataAsync(stubChainId, newMockSideChainBlockData, Hash.Default, 1);
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
            var fakeCache = new Dictionary<int, List<IBlockInfo> >{{stubChainId, mockParentChainBlockData}};
            var parentChainIdHeights = new Dictionary<int, ulong>
            {
                {stubChainId, 1}
            };
            var crossChainService = CreateNewCrossChainService(fakeCache, new Dictionary<int, ulong>(), parentChainIdHeights);
            var res = await crossChainService.ValidateParentChainBlockDataAsync(stubChainId,
                mockParentChainBlockData.Select(pcb => (ParentChainBlockData) pcb).ToList(), Hash.Default, 1);
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
            
            var fakeCache = new Dictionary<int, List<IBlockInfo> >{{stubChainId, mockParentChainBlockData}};
            var parentChainIdHeights = new Dictionary<int, ulong>
            {
                {stubChainId, 1}
            };
            var crossChainService = CreateNewCrossChainService(fakeCache, new Dictionary<int, ulong>(), parentChainIdHeights);
            var res = await crossChainService.ValidateParentChainBlockDataAsync(stubChainId, newMockParentChainBlockData, Hash.Default, 1);
            Assert.False(res);
        }
    }
}