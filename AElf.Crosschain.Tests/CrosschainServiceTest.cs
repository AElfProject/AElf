using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.CrossChain;
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
            mockObject.Setup(m => m.GetSideChainBlockDataAsync(It.IsAny<int>(), It.IsAny<IList<SideChainBlockData>>(), It.IsAny<Hash>(), It.IsAny<ulong>(), false))
                .Returns<int, IList<SideChainBlockData>, Hash, ulong, bool>(
                    (chainId, input, preBlockHash, preBlockHeight, isValidation) =>
                    {
                        foreach (var blockData in sideChainBlockData)
                        {
                            input.Add(blockData);
                        }
                        return Task.FromResult(true);
                    });
            mockObject.Setup(m => m.GetSideChainBlockDataAsync(It.IsAny<int>(), It.IsAny<IList<SideChainBlockData>>(), It.IsAny<Hash>(), It.IsAny<ulong>(), true))
                .Returns<int, IList<SideChainBlockData>, Hash, ulong, bool>(
                    (chainId, input, preBlockHash, preBlockHeight, isValidation) => Task.FromResult(input.Equals(sideChainBlockData)));
            
            mockObject.Setup(m => m.GetParentChainBlockDataAsync(It.IsAny<int>(), It.IsAny<IList<ParentChainBlockData>>(), It.IsAny<Hash>(), It.IsAny<ulong>(), false))
                .Returns<int, IList<ParentChainBlockData>, Hash, ulong, bool>(
                    (chainId, input, preBlockHash, preBlockHeight, isValidation) =>
                    {
                        foreach (var blockData in parentChainBlockData)
                        {
                            input.Add(blockData);
                        }
                        return Task.FromResult(true);
                    });
            
            mockObject.Setup(m => m.GetParentChainBlockDataAsync(It.IsAny<int>(), It.IsAny<IList<ParentChainBlockData>>(), It.IsAny<Hash>(), It.IsAny<ulong>(), true))
                .Returns<int, IList<ParentChainBlockData>, Hash, ulong, bool>(
                    (chainId, input, preBlockHash, preBlockHeight, isValidation) => Task.FromResult(input.Equals(parentChainBlockData)));

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
            var actual = await crossChainService.GetSideChainBlockDataAsync(stubChainId, Hash.Default, 1);
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
                    SideChainId = stubChainId
                }
            };
            var stubCrossChainCrossChainDataProvider =
                CreateFakeCrossChainDataProvider(mockSideChainBlockData, null);
            var crossChainService = new CrossChainService(stubCrossChainCrossChainDataProvider);
            var actual = await crossChainService.GetSideChainBlockDataAsync(stubChainId, Hash.Default, 1);
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
            var actual = await crossChainService.GetParentChainBlockDataAsync(stubChainId, Hash.Default, 1);
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
                        ParentChainId = stubChainId,
                        ParentChainHeight = 1
                    }
                }
            };
            var stubCrossChainCrossChainDataProvider =
                CreateFakeCrossChainDataProvider(null, mockParentChainBlockData);
            var crossChainService = new CrossChainService(stubCrossChainCrossChainDataProvider);
            var actual = await crossChainService.GetParentChainBlockDataAsync(stubChainId, Hash.Default, 1);
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
                    SideChainId = stubChainId
                }
            };
            var stubCrossChainCrossChainDataProvider =
                CreateFakeCrossChainDataProvider(mockSideChainBlockData, null);
            var crossChainService = new CrossChainService(stubCrossChainCrossChainDataProvider);
            var actual = await crossChainService.ValidateSideChainBlockDataAsync(stubChainId, mockSideChainBlockData, Hash.Default, 1);
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
                    SideChainId = stubChainId,
                    SideChainHeight = 1
                }
            };
            
            var stubCrossChainCrossChainDataProvider =
                CreateFakeCrossChainDataProvider(mockSideChainBlockData, null);
            var crossChainService = new CrossChainService(stubCrossChainCrossChainDataProvider);
            var mockSideChainBlockData_new = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    SideChainId = stubChainId + 1,
                    SideChainHeight = 2
                }
            }; 
            
            var actual = await crossChainService.ValidateSideChainBlockDataAsync(stubChainId, mockSideChainBlockData_new, Hash.Default, 1);
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
                        ParentChainId = stubChainId,
                        ParentChainHeight = 1
                    }
                }
            };
            var stubCrossChainCrossChainDataProvider =
                CreateFakeCrossChainDataProvider(null, mockParentChainBlockData);
            var crossChainService = new CrossChainService(stubCrossChainCrossChainDataProvider);
            var actual = await crossChainService.ValidateParentChainBlockDataAsync(stubChainId, mockParentChainBlockData, Hash.Default, 1);
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
                        ParentChainId = stubChainId,
                        ParentChainHeight = 1
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
                        ParentChainId = stubChainId,
                        ParentChainHeight = 2
                    }
                }
            };
            var actual = await crossChainService.ValidateParentChainBlockDataAsync(stubChainId, mockParentChainBlockData_new, Hash.Default, 1);
            Assert.False(actual);
        }
    }
}