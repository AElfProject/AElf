using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Crosschain.EventMessage;
using AElf.Crosschain.Grpc.Client;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using Moq;
using Volo.Abp.EventBus.Local;
using Xunit;
using Xunit.Abstractions;

namespace AElf.Crosschain
{
    public class CrosschainDataProviderTest : CrosschainTestBase
    {
        private readonly ITestOutputHelper _testOutputHelper;

        private IClientBase FakeCLientBase(int chainId)
        {
            return new GrpcClientBase
            {
                BlockInfoCache = new BlockInfoCache(chainId)
            };
        }

        public CrosschainDataProviderTest(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        private ICrossChainDataProvider NewCrossChainDataProvider()
        {
            var mockClientService = CreateFakeClientService();
            var crossChainDataProvider = new CrossChainDataProvider(mockClientService, SmartContractExecutiveService, AccountService);
            //crossChainDataProvider.LocalEventBus = _localEventBus;
            LocalEventBus.Subscribe<NewSideChainConnectionReceivedEvent>(
                e => crossChainDataProvider.HandleEventAsync(e));
            LocalEventBus.Subscribe<NewParentChainConnectionEvent>(e => crossChainDataProvider.HandleEventAsync(e));
            return crossChainDataProvider;
        }

        private IClientBase CreateFakeClientBase(int chainId)
        {
            var blockInfoCache = new BlockInfoCache(chainId);
            Mock<IClientBase> mockObject = new Mock<IClientBase>();
            mockObject.Setup(m => m.BlockInfoCache).Returns(blockInfoCache);
            return mockObject.Object;
        }
        
        private IClientService CreateFakeClientService()
        {
            Mock<IClientService> mockObject = new Mock<IClientService>();
            return mockObject.Object;
        }
        
        [Fact]
        public async Task Arrange_NewSideChainConnectionEvent()
        {
            int chainId = 123;
            var crossChainDataProvider = NewCrossChainDataProvider();
            var mockClientBase = CreateFakeClientBase(chainId);
            
            await LocalEventBus.PublishAsync(new NewSideChainConnectionReceivedEvent
            {
                ClientBase = mockClientBase
            });
            Assert.True(crossChainDataProvider.GetCachedChainCount() == 1);
        }

        #region Side chain
        
        [Fact]
        public async Task GetSideChainBlock_WithoutCache()
        {
            int chainId = 123;
            var crossChainDataProvider = NewCrossChainDataProvider();

            var mockClientBase = CreateFakeClientBase(chainId);
            
            await LocalEventBus.PublishAsync(new NewSideChainConnectionReceivedEvent
            {
                ClientBase = mockClientBase
            });
            var list = new List<SideChainBlockData>();
            var res = await crossChainDataProvider.GetSideChainBlockData(list);
            Assert.False(res);
            Assert.True(list.Count == 0);
        }

        [Fact]
        public async Task GetSideChainBlock_WithoutEnoughCache()
        {
            int chainId = 123;
            var crossChainDataProvider = NewCrossChainDataProvider();

            var mockClientBase = CreateFakeClientBase(chainId);
            
            await LocalEventBus.PublishAsync(new NewSideChainConnectionReceivedEvent
            {
                ClientBase = mockClientBase
            });

            mockClientBase.BlockInfoCache.TryAdd(new SideChainBlockData
            {
                Height = 1
            });
            var list = new List<SideChainBlockData>();
            var res = await crossChainDataProvider.GetSideChainBlockData(list);
            Assert.False(res);
            Assert.True(list.Count == 0);
        }

        [Fact]
        public async Task GetSideChainBlock_WithEnoughCache()
        {
            int chainId = 123;
            var crossChainDataProvider = NewCrossChainDataProvider();

            var mockClientBase = CreateFakeClientBase(chainId);
            
            await LocalEventBus.PublishAsync(new NewSideChainConnectionReceivedEvent
            {
                ClientBase = mockClientBase
            });

            for (int i = 0; i <= CrossChainConsts.MinimalBlockInfoCacheThreshold; i++)
            {
                mockClientBase.BlockInfoCache.TryAdd(new SideChainBlockData
                {
                    Height = (ulong) (1 + i)
                });
            }
            
            var list = new List<SideChainBlockData>();
            var res = await crossChainDataProvider.GetSideChainBlockData(list);
            Assert.True(res);
            Assert.True(list.Count == 1);
        }

        [Fact]
        public async Task Validate_Without_ProvidedSideChainBlockData()
        {
            int chainId = 123;
            var crossChainDataProvider = NewCrossChainDataProvider();

            var mockClientBase = CreateFakeClientBase(chainId);
            
            await LocalEventBus.PublishAsync(new NewSideChainConnectionReceivedEvent
            {
                ClientBase = mockClientBase
            });
            
            mockClientBase.BlockInfoCache.TryAdd(new SideChainBlockData
            {
                ChainId = 123,
                Height = 1
            });
            
            var list = new List<SideChainBlockData>();
            var res = await crossChainDataProvider.GetSideChainBlockData(list, true);
            Assert.True(res);
        }
        
        
        [Fact]
        public async Task ValidateSideChainBlock_WithCaching()
        {
            int chainId = 123;
            var crossChainDataProvider = NewCrossChainDataProvider();

            var mockClientBase = CreateFakeClientBase(chainId);
            
            await LocalEventBus.PublishAsync(new NewSideChainConnectionReceivedEvent
            {
                ClientBase = mockClientBase
            });
            
            mockClientBase.BlockInfoCache.TryAdd(new SideChainBlockData
            {
                ChainId = 123,
                Height = 1
            });
            
            var list = new List<SideChainBlockData>{new SideChainBlockData
            {
                ChainId = 123,
                Height = 1
            }};
            var res = await crossChainDataProvider.GetSideChainBlockData(list, true);
            Assert.True(res);
            Assert.True(list.Count == 1);
        }
        
        [Fact]
        public async Task ValidateSideChainBlock_WithoutCaching()
        {
            var crossChainDataProvider = NewCrossChainDataProvider();
            
            var list = new List<SideChainBlockData>{new SideChainBlockData
            {
                ChainId = 123,
                Height = 1
            }};
            var res = await crossChainDataProvider.GetSideChainBlockData(list, true);
            Assert.False(res);
        }
        
        [Fact]
        public async Task ValidateSideChainBlock_WithWrongBlockIndex()
        {
            int chainId = 123;
            var crossChainDataProvider = NewCrossChainDataProvider();

            var mockClientBase = CreateFakeClientBase(chainId);
            
            await LocalEventBus.PublishAsync(new NewSideChainConnectionReceivedEvent
            {
                ClientBase = mockClientBase
            });
            
            mockClientBase.BlockInfoCache.TryAdd(new SideChainBlockData
            {
                ChainId = 123,
                Height = 1
            });
            
            var list = new List<SideChainBlockData>{new SideChainBlockData
            {
                ChainId = 123,
                Height = 2
            }};
            var res = await crossChainDataProvider.GetSideChainBlockData(list, true);
            Assert.False(res);
            Assert.True(list.Count == 1);
        }
        
        [Fact]
        public async Task ValidateSideChainBlock__NotEnoughCaching()
        {
            int chainId = 123;
            var crossChainDataProvider = NewCrossChainDataProvider();

            var mockClientBase = CreateFakeClientBase(chainId);
            
            await LocalEventBus.PublishAsync(new NewSideChainConnectionReceivedEvent
            {
                ClientBase = mockClientBase
            });
            
            var list = new List<SideChainBlockData>{new SideChainBlockData
            {
                ChainId = 123,
                Height = 1
            }};
            var res = await crossChainDataProvider.GetSideChainBlockData(list, true);
            Assert.False(res);
            Assert.True(list.Count == 1);
        }

        [Fact]
        public async Task TryTwice_GetSideChainBlockData()
        {
            int chainId = 123;
            var crossChainDataProvider = NewCrossChainDataProvider();

            var mockClientBase = CreateFakeClientBase(chainId);
            
            await LocalEventBus.PublishAsync(new NewSideChainConnectionReceivedEvent
            {
                ClientBase = mockClientBase
            });

            for (int i = 0; i <= CrossChainConsts.MinimalBlockInfoCacheThreshold; i++)
            {
                mockClientBase.BlockInfoCache.TryAdd(new SideChainBlockData
                {
                    Height = (ulong) (1 + i)
                });
            }
            
            var list = new List<SideChainBlockData>();
            await crossChainDataProvider.GetSideChainBlockData(list);
            list = new List<SideChainBlockData>();
            var res = await crossChainDataProvider.GetSideChainBlockData(list);
            Assert.True(res);
            Assert.True(list.Count == 1);
        }
        
        #endregion

        #region Parent chain

        [Fact]
        public async Task Arrange_NullParentChain()
        {
            int chainId = 123;
            var crossChainDataProvider = NewCrossChainDataProvider();

//            var mockClientBase = CreateFakeClientBase(chainId);
//            await LocalEventBus.PublishAsync(new NewParentChainConnectionEvent
//            {
//                ClientBase = mockClientBase
//            });

            var list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockData(list);
            Assert.False(res);
        }

        [Fact]
        public async Task GetParentChainBLock_WithCache()
        {
            int chainId = 123;
            var crossChainDataProvider = NewCrossChainDataProvider();

            var mockClientBase = CreateFakeClientBase(chainId);
            await LocalEventBus.PublishAsync(new NewParentChainConnectionEvent
            {
                ClientBase = mockClientBase
            });

            var list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockData(list);
            Assert.True(res);
            Assert.True(list.Count == 0);
        }

        [Fact]
        public async Task ValidateParentChainBlock_WithoutProvidedData()
        {
            int chainId = 123;
            var crossChainDataProvider = NewCrossChainDataProvider();

            var mockClientBase = CreateFakeClientBase(chainId);
            await LocalEventBus.PublishAsync(new NewParentChainConnectionEvent
            {
                ClientBase = mockClientBase
            });

            var list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockData(list, true);
            Assert.True(res);
        }
        
        [Fact]
        public async Task ValidateParentChainBlock_WithTooManyProvidedData()
        {
            int chainId = 123;
            var crossChainDataProvider = NewCrossChainDataProvider();

            var mockClientBase = CreateFakeClientBase(chainId);
            await LocalEventBus.PublishAsync(new NewParentChainConnectionEvent
            {
                ClientBase = mockClientBase
            });

            var list = new List<ParentChainBlockData>();
            for (int i = 0; i <= CrossChainConsts.MaximalCountForIndexingParentChainBlock; i++)
            {
                list.Add(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ChainId = chainId,
                        Height = (ulong) (i +1)
                    }
                });
            }
            var res = await crossChainDataProvider.GetParentChainBlockData(list, true);
            Assert.False(res);
        }

        [Fact]
        public async Task ValidateParentChainBlock__WithWrongIndex()
        {
            int chainId = 123;
            var crossChainDataProvider = NewCrossChainDataProvider();

            var mockClientBase = CreateFakeClientBase(chainId);
            await LocalEventBus.PublishAsync(new NewParentChainConnectionEvent
            {
                ClientBase = mockClientBase
            });
            var cachingCount = 5;
            for (int i = 0; i < cachingCount; i++)
            {
                mockClientBase.BlockInfoCache.TryAdd(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ChainId = chainId,
                        Height = (ulong) (i +1)
                    }
                });
            }

            var list = new List<ParentChainBlockData>
            {
                new ParentChainBlockData {Root = new ParentChainBlockRootInfo {ChainId = chainId, Height = 2}}
            };
            var res = await crossChainDataProvider.GetParentChainBlockData(list, true);
            Assert.False(res);
        }
        
        [Fact]
        public async Task GetParentChainBlock_WithProvidedData()
        {
            int chainId = 123;
            var crossChainDataProvider = NewCrossChainDataProvider();

            var mockClientBase = CreateFakeClientBase(chainId);
            await LocalEventBus.PublishAsync(new NewParentChainConnectionEvent
            {
                ClientBase = mockClientBase
            });
            mockClientBase.BlockInfoCache.TryAdd(new ParentChainBlockData
            {
                Root = new ParentChainBlockRootInfo
                {
                    ChainId = chainId,
                    Height = 1
                }
            });

            var list = new List<ParentChainBlockData>
            {
                new ParentChainBlockData {Root = new ParentChainBlockRootInfo {ChainId = chainId, Height = 1}}
            };
            var res = await crossChainDataProvider.GetParentChainBlockData(list);
            Assert.False(res);
        }
        
        [Fact]
        public async Task GetSingleParentBlock_Single()
        {
            int chainId = 123;
            var crossChainDataProvider = NewCrossChainDataProvider();

            var mockClientBase = CreateFakeClientBase(chainId);
            await LocalEventBus.PublishAsync(new NewParentChainConnectionEvent
            {
                ClientBase = mockClientBase
            });
            var cachingCount = 5;
            for (int i = 0; i < cachingCount; i++)
            {
                mockClientBase.BlockInfoCache.TryAdd(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ChainId = chainId,
                        Height = (ulong) (i +1)
                    }
                });
            }

            var list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockData(list);
            Assert.True(res);
            var expectedResultCount = cachingCount - CrossChainConsts.MinimalBlockInfoCacheThreshold;
            Assert.True(list.Count == expectedResultCount);
        }

        [Fact]
        public async Task GetParentChainBlock_Success()
        {
            int chainId = 123;
            var crossChainDataProvider = NewCrossChainDataProvider();

            var mockClientBase = CreateFakeClientBase(chainId);
            await LocalEventBus.PublishAsync(new NewParentChainConnectionEvent
            {
                ClientBase = mockClientBase
            });
            var cachingCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            for (int i = 0; i < cachingCount; i++)
            {
                mockClientBase.BlockInfoCache.TryAdd(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ChainId = chainId,
                        Height = (ulong) (i +1)
                    }
                });
            }

            var list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockData(list);
            Assert.True(res);
            var expectedResultCount = cachingCount - CrossChainConsts.MinimalBlockInfoCacheThreshold;
            Assert.True(list.Count == expectedResultCount);
        }
        
        [Fact]
        public async Task GetParentChainBlock_WithCountLimit()
        {
            int chainId = 123;
            var crossChainDataProvider = NewCrossChainDataProvider();

            var mockClientBase = CreateFakeClientBase(chainId);
            await LocalEventBus.PublishAsync(new NewParentChainConnectionEvent
            {
                ClientBase = mockClientBase
            });
            var cachingCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            for (int i = 0; i <= cachingCount + CrossChainConsts.MinimalBlockInfoCacheThreshold; i++)
            {
                mockClientBase.BlockInfoCache.TryAdd(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ChainId = chainId,
                        Height = (ulong) (i +1)
                    }
                });
            }

            var list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockData(list);
            Assert.True(res);
            var expectedResultCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            Assert.True(list.Count == expectedResultCount);
        }

        [Fact]
        public async Task TryTwice_GetParentChainBlock_WithCountLimit()
        {
            int chainId = 123;
            var crossChainDataProvider = NewCrossChainDataProvider();

            var mockClientBase = CreateFakeClientBase(chainId);
            await LocalEventBus.PublishAsync(new NewParentChainConnectionEvent
            {
                ClientBase = mockClientBase
            });
            var cachingCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            for (int i = 0; i <= cachingCount + CrossChainConsts.MinimalBlockInfoCacheThreshold; i++)
            {
                mockClientBase.BlockInfoCache.TryAdd(new ParentChainBlockData
                {
                    Root = new ParentChainBlockRootInfo
                    {
                        ChainId = chainId,
                        Height = (ulong) (i +1)
                    }
                });
            }

            var list = new List<ParentChainBlockData>();
            await crossChainDataProvider.GetParentChainBlockData(list);
            list = new List<ParentChainBlockData>();
            var res = await crossChainDataProvider.GetParentChainBlockData(list);
            Assert.True(res);
            var expectedResultCount = CrossChainConsts.MaximalCountForIndexingParentChainBlock;
            Assert.True(list.Count == expectedResultCount);
        }

//        [Fact]
//        public async Task Act_Twice_GetParentChainBlock()
//        {
//            int chainId = 123;
//            var crossChainDataProvider = NewCrossChainDataProvider();
//
//            var mockClientBase = CreateFakeClientBase(chainId);
//            await LocalEventBus.PublishAsync(new NewParentChainConnectionEvent
//            {
//                ClientBase = mockClientBase
//            });
//            
//            var cachingCount = CrossChainConsts.MinimalBlockInfoCacheThreshold + 2;
//            for (int i = 0; i <= cachingCount + CrossChainConsts.MinimalBlockInfoCacheThreshold; i++)
//            {
//                mockClientBase.BlockInfoCache.TryAdd(new ParentChainBlockData
//                {
//                    Root = new ParentChainBlockRootInfo
//                    {
//                        ChainId = chainId,
//                        Height = (ulong) (i +1)
//                    }
//                });
//            }
//            
//            var list = new List<ParentChainBlockData>
//            {
//                new ParentChainBlockData
//                {
//                    Root = new ParentChainBlockRootInfo
//                    {
//                        ChainId = chainId,
//                        Height = 1
//                    }
//                }
//            };
//            await crossChainDataProvider.GetParentChainBlockData(list, true);
//            list = new List<ParentChainBlockData>();
//            var res =  await crossChainDataProvider.GetParentChainBlockData(list);
//            Assert.True(res);
//            Assert.True(list.Count == 1);
//        }
        
        #endregion
    }
}