using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using Moq;
using Xunit;

namespace AElf.Crosschain
{
    public class CrosschainServiceTest : CrosschainTestBase
    {
        private ICrossChainDataProvider CreateFakeCrossChainDataProvider(IList<SideChainBlockData> sideChainBlockData, 
            List<ParentChainBlockData> parentChainBlockData)
        {
            Mock<ICrossChainDataProvider> mockObject = new Mock<ICrossChainDataProvider>();
            mockObject.Setup(m => m.GetSideChainBlockDataAsync(It.IsAny<int>(), It.IsAny<IList<SideChainBlockData>>(), false))
                .Returns<int, IList<SideChainBlockData>, bool>(
                    (chainId, input, isValidation) =>
                    {
                        foreach (var blockData in sideChainBlockData)
                        {
                            input.Add(blockData);
                        }
                        return Task.FromResult(true);
                    });
            mockObject.Setup(m => m.GetSideChainBlockDataAsync(It.IsAny<int>(), It.IsAny<IList<SideChainBlockData>>(), true))
                .Returns<int, IList<SideChainBlockData>, bool>(
                    (chainId, input, isValidation) => Task.FromResult(input.Equals(sideChainBlockData)));
            
            mockObject.Setup(m => m.GetParentChainBlockDataAsync(It.IsAny<int>(), It.IsAny<IList<ParentChainBlockData>>(), false))
                .Returns<int, IList<ParentChainBlockData>, bool>(
                    (chainId, input, isValidation) =>
                    {
                        foreach (var blockData in parentChainBlockData)
                        {
                            input.Add(blockData);
                        }
                        return Task.FromResult(true);
                    });
            
            mockObject.Setup(m => m.GetParentChainBlockDataAsync(It.IsAny<int>(), It.IsAny<IList<ParentChainBlockData>>(), true))
                .Returns<int, IList<ParentChainBlockData>, bool>(
                    (chainId, input, isValidation) => Task.FromResult(input.Equals(parentChainBlockData)));

            return mockObject.Object;
        }

        [Fact]
        public async Task GetSideChainBlock_WithoutCache()
        {
            int stubChainId = 123;
            var mockSideChainBlockData = new List<SideChainBlockData>();
            var stubCrossChainCrossChainDataProvider =
                CreateFakeCrossChainDataProvider(mockSideChainBlockData, null);
            var crossChainService = new CrossChainService(stubCrossChainCrossChainDataProvider);
            var actual = await crossChainService.GetSideChainBlockDataAsync(stubChainId);
            Assert.Equal(mockSideChainBlockData, actual);
        }
        
        [Fact]
        public async Task GetSideChainBlock_WithCache()
        {
            int stubChainId = 123;
            var mockSideChainBlockData = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    ChainId = stubChainId
                }
            };
            var stubCrossChainCrossChainDataProvider =
                CreateFakeCrossChainDataProvider(mockSideChainBlockData, null);
            var crossChainService = new CrossChainService(stubCrossChainCrossChainDataProvider);
            var actual = await crossChainService.GetSideChainBlockDataAsync(stubChainId);
            Assert.Equal(mockSideChainBlockData, actual);
        }
        
        [Fact]
        public async Task GetParentChainBlock_WithoutCache()
        {
            int stubChainId = 123;
            var mockParentChainBlockData = new List<ParentChainBlockData>();
            var stubCrossChainCrossChainDataProvider =
                CreateFakeCrossChainDataProvider(null, mockParentChainBlockData);
            var crossChainService = new CrossChainService(stubCrossChainCrossChainDataProvider);
            var actual = await crossChainService.GetParentChainBlockDataAsync(stubChainId);
            Assert.Equal(mockParentChainBlockData, actual);
        }
        
        [Fact]
        public async Task GetParentChainBlock_WithCache()
        {
            int stubChainId = 123;
            var mockParentChainBlockData = new List<ParentChainBlockData>
            {
                new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ChainId = stubChainId,
                        Height = 1
                    }
                }
            };
            var stubCrossChainCrossChainDataProvider =
                CreateFakeCrossChainDataProvider(null, mockParentChainBlockData);
            var crossChainService = new CrossChainService(stubCrossChainCrossChainDataProvider);
            var actual = await crossChainService.GetParentChainBlockDataAsync(stubChainId);
            Assert.Equal(mockParentChainBlockData, actual);
        }
        
        [Fact]
        public async Task ValidateSideChainBlock_Success()
        {
            int stubChainId = 123;
            var mockSideChainBlockData = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    ChainId = stubChainId
                }
            };
            var stubCrossChainCrossChainDataProvider =
                CreateFakeCrossChainDataProvider(mockSideChainBlockData, null);
            var crossChainService = new CrossChainService(stubCrossChainCrossChainDataProvider);
            var actual = await crossChainService.ValidateSideChainBlockDataAsync(stubChainId, mockSideChainBlockData);
            Assert.True(actual);
        }
        
        [Fact]
        public async Task ValidateSideChainBlock_Fail()
        {
            int stubChainId = 123;
            var mockSideChainBlockData = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    ChainId = stubChainId
                }
            };
            
            var stubCrossChainCrossChainDataProvider =
                CreateFakeCrossChainDataProvider(mockSideChainBlockData, null);
            var crossChainService = new CrossChainService(stubCrossChainCrossChainDataProvider);
            var mockSideChainBlockData_new = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    ChainId = stubChainId + 1
                }
            }; 
            
            var actual = await crossChainService.ValidateSideChainBlockDataAsync(stubChainId, mockSideChainBlockData_new);
            Assert.False(actual);
        }
        
        [Fact]
        public async Task ValidateParentChainBlock_Success()
        {
            int stubChainId = 123;
            var mockParentChainBlockData = new List<ParentChainBlockData>
            {
                new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ChainId = stubChainId,
                        Height = 1
                    }
                }
            };
            var stubCrossChainCrossChainDataProvider =
                CreateFakeCrossChainDataProvider(null, mockParentChainBlockData);
            var crossChainService = new CrossChainService(stubCrossChainCrossChainDataProvider);
            var actual = await crossChainService.ValidateParentChainBlockDataAsync(stubChainId, mockParentChainBlockData);
            Assert.True(actual);
        }
        
        [Fact]
        public async Task ValidateParentChainBlock_Fail()
        {
            int stubChainId = 123;
            var mockParentChainBlockData = new List<ParentChainBlockData>
            {
                new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ChainId = stubChainId,
                        Height = 1
                    }
                }
            };
            var stubCrossChainCrossChainDataProvider =
                CreateFakeCrossChainDataProvider(null, mockParentChainBlockData);
            var crossChainService = new CrossChainService(stubCrossChainCrossChainDataProvider);
            var mockParentChainBlockData_new = new List<ParentChainBlockData>
            {
                new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ChainId = stubChainId,
                        Height = 2
                    }
                }
            };
            var actual = await crossChainService.ValidateParentChainBlockDataAsync(stubChainId, mockParentChainBlockData_new);
            Assert.False(actual);
        }
    }
}