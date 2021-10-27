using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Standards.ACS7;
using AElf.CrossChain.Indexing.Application;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.CrossChain
{
    public class CrossChainIndexingDataValidationServiceTests : CrossChainTestBase
    {
        private readonly CrossChainTestHelper _crossChainTestHelper;
        private readonly ICrossChainIndexingDataValidationService _crossChainIndexingDataValidationService;

        public CrossChainIndexingDataValidationServiceTests()
        {
            _crossChainTestHelper = GetRequiredService<CrossChainTestHelper>();
            _crossChainIndexingDataValidationService = GetRequiredService<ICrossChainIndexingDataValidationService>();
        }

        [Fact]
        public async Task Validate_WithoutEmptyInput_Test()
        {
            int chainId = _chainOptions.ChainId;
            var blockInfoCache = new List<ICrossChainBlockEntity>();
            for (int i = 0; i <= CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                blockInfoCache.Add(new SideChainBlockData
                {
                    Height = 1 + i,
                    ChainId = chainId
                });
            }

            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 0);
            var fakeCache = new Dictionary<int, List<ICrossChainBlockEntity>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var crossChainBlockData = new CrossChainBlockData();
            var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingDataAsync(crossChainBlockData,
                Hash.Empty, 1);
            Assert.True(res);
        }


        [Fact]
        public async Task ValidateSideChainBlock_WithCaching_Test()
        {
            int chainId = _chainOptions.ChainId;
            var blockInfoCache = new List<ICrossChainBlockEntity>
            {
                new SideChainBlockData
                    {ChainId = chainId, Height = 1, TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom("1")}
            };
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 0);

            var fakeCache = new Dictionary<int, List<ICrossChainBlockEntity>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);


            var crossChainBlockData = FakeCrossChainBlockData(new[]
            {
                new SideChainBlockData
                {
                    ChainId = chainId,
                    Height = 1,
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom("1")
                }
            }, new ParentChainBlockData[0]);

            var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingDataAsync(crossChainBlockData,
                Hash.Empty, 1);
            Assert.True(res);
        }

        [Fact]
        public async Task ValidateSideChainBlock_WithoutCaching_Test()
        {
            int chainId = _chainOptions.ChainId;

            var list = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    ChainId = chainId,
                    Height = 1
                }
            };
            var crossChainBlockData = FakeCrossChainBlockData(list, new ParentChainBlockData[0]);
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 1);
            var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingDataAsync(crossChainBlockData,
                Hash.Empty, 1);
            Assert.False(res);
        }


        [Fact]
        public async Task ValidateSideChainBlock_WithWrongBlockIndex_Test()
        {
            int chainId = _chainOptions.ChainId;
            var blockInfoCache = new List<ICrossChainBlockEntity>
            {
                new SideChainBlockData {ChainId = chainId, Height = 1}
            };

            var fakeCache = new Dictionary<int, List<ICrossChainBlockEntity>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 1);
            var list = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    ChainId = chainId,
                    Height = 2
                }
            };
            var crossChainBlockData = FakeCrossChainBlockData(list, new ParentChainBlockData[0]);
            var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingDataAsync(crossChainBlockData,
                Hash.Empty, 1);
            Assert.False(res);
        }


        [Fact]
        public async Task ValidateSideChainBlock__NotEnoughCaching_Test()
        {
            int chainId = _chainOptions.ChainId;
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 1);

            var list = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    ChainId = chainId,
                    Height = 1
                }
            };
            var crossChainBlockData = new CrossChainBlockData
            {
                SideChainBlockDataList = {list}
            };
            var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingDataAsync(crossChainBlockData,
                Hash.Empty, 1);
            Assert.False(res);
        }


        [Fact]
        public async Task ValidateSideChainBlock__NotExpected_Test()
        {
            int chainId = _chainOptions.ChainId;
            var blockInfoCache = new List<ICrossChainBlockEntity>
            {
                new SideChainBlockData
                {
                    ChainId = chainId,
                    Height = 1,
                    BlockHeaderHash = HashHelper.ComputeFrom("blockHash")
                }
            };
            _crossChainTestHelper.AddFakeSideChainIdHeight(chainId, 0);
            var fakeCache = new Dictionary<int, List<ICrossChainBlockEntity>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var list = new List<SideChainBlockData>
            {
                new SideChainBlockData
                {
                    ChainId = chainId,
                    Height = 1,
                    BlockHeaderHash = HashHelper.ComputeFrom("Block")
                }
            };
            var crossChainBlockData = FakeCrossChainBlockData(list, new ParentChainBlockData[0]);
            var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingDataAsync(crossChainBlockData,
                Hash.Empty, 1);
            Assert.False(res);
        }

        [Fact]
        public async Task TryTwice_ValidateSideChainBlock_Test()
        {
            int chainId = _chainOptions.ChainId;
            var blockInfoCache = new List<ICrossChainBlockEntity>();
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 0);

            var cachingCount = CrossChainConstants.DefaultBlockCacheEntityCount * 2;
            for (int i = 0; i <= cachingCount; i++)
            {
                blockInfoCache.Add(new SideChainBlockData()
                {
                    ChainId = chainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom((i + 1).ToString())
                });
            }

            var fakeCache = new Dictionary<int, List<ICrossChainBlockEntity>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);
            {
                var list = new List<SideChainBlockData>();
                for (int i = 0; i < CrossChainConstants.DefaultBlockCacheEntityCount; i++)
                {
                    list.Add(new SideChainBlockData
                    {
                        ChainId = chainId,
                        Height = (i + 1),
                        TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom((i + 1).ToString())
                    });
                }

                var crossChainBlockData = FakeCrossChainBlockData(list, new ParentChainBlockData[0]);
                var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingDataAsync(
                    crossChainBlockData,
                    Hash.Empty, 1);
                Assert.True(res);
            }
            {
                var list = new List<SideChainBlockData>();
                for (int i = 0; i < CrossChainConstants.DefaultBlockCacheEntityCount; i++)
                {
                    list.Add(new SideChainBlockData
                    {
                        ChainId = chainId,
                        Height = (i + 1),
                        TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom((i + 1).ToString())
                    });
                }

                var crossChainBlockData = FakeCrossChainBlockData(list, new ParentChainBlockData[0]);
                var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingDataAsync(
                    crossChainBlockData,
                    Hash.Empty, 2);
                Assert.True(res);
            }
        }

        [Fact]
        public async Task ValidateParentChainBlock_WithoutProvidedData_Test()
        {
            int chainId = _chainOptions.ChainId;
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 0);

            var crossChainBlockData = FakeCrossChainBlockData(new SideChainBlockData[0], new ParentChainBlockData[0]);
            var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingDataAsync(crossChainBlockData,
                Hash.Empty, 1);
            Assert.True(res);
        }

        [Fact]
        public async Task ValidateParentChainBlock_WithCaching_Test()
        {
            int chainId = _chainOptions.ChainId;
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 0);

            var blockInfoCache = new List<ICrossChainBlockEntity>();
            for (int i = 0; i < CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData
                {
                    ChainId = chainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom(i + 1)
                });
            }

            var fakeCache = new Dictionary<int, List<ICrossChainBlockEntity>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            {
                var list = new List<ParentChainBlockData>();
                for (int i = 0; i < CrossChainConstants.DefaultBlockCacheEntityCount; i++)
                {
                    list.Add(new ParentChainBlockData
                    {
                        ChainId = chainId,
                        Height = (i + 1),
                        TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom(i)
                    });
                }

                var crossChainBlockData = FakeCrossChainBlockData(new SideChainBlockData[0], list);
                var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingDataAsync(
                    crossChainBlockData,
                    Hash.Empty, 1);
                res.ShouldBeFalse();
            }
            
            {
                var list = new List<ParentChainBlockData>();
                for (int i = 0; i < CrossChainConstants.DefaultBlockCacheEntityCount; i++)
                {
                    list.Add(new ParentChainBlockData
                    {
                        ChainId = chainId,
                        Height = (i + 1),
                        TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom(i + 1)
                    });
                }

                var crossChainBlockData = FakeCrossChainBlockData(new SideChainBlockData[0], list);
                var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingDataAsync(
                    crossChainBlockData,
                    Hash.Empty, 1);
                Assert.True(res);
            }
        }

        [Fact]
        public async Task ValidateParentChainBlock_WithoutCaching_Test()
        {
            int parentChainId = _chainOptions.ChainId;
            _crossChainTestHelper.AddFakeParentChainIdHeight(parentChainId, 1);

            var list = new List<ParentChainBlockData>();
            for (int i = 0; i <= CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                list.Add(new ParentChainBlockData
                {
                    ChainId = parentChainId,
                    Height = (i + 1)
                });
            }

            var crossChainBlockData = FakeCrossChainBlockData(new SideChainBlockData[0], list);
            var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingDataAsync(crossChainBlockData,
                Hash.Empty, 1);
            Assert.False(res);
        }

        [Fact]
        public async Task ValidateParentChainBlock_WithWrongIndex_Test()
        {
            int chainId = _chainOptions.ChainId;
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 0);
            var blockInfoCache = new List<ICrossChainBlockEntity>();
            var cachingCount = 5;
            for (int i = 0; i < cachingCount; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData
                {
                    ChainId = chainId,
                    Height = (i + 1)
                });
            }

            var fakeCache = new Dictionary<int, List<ICrossChainBlockEntity>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);

            var list = new List<ParentChainBlockData>
            {
                new ParentChainBlockData
                    {ChainId = chainId, Height = 2}
            };
            var crossChainBlockData = FakeCrossChainBlockData(new SideChainBlockData[0], list);
            var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingDataAsync(crossChainBlockData,
                Hash.Empty, 1);
            Assert.False(res);
        }

        [Fact]
        public async Task TryTwice_ValidateParentChainBlock_Test()
        {
            int chainId = _chainOptions.ChainId;
            var blockInfoCache = new List<ICrossChainBlockEntity>();
            _crossChainTestHelper.AddFakeParentChainIdHeight(chainId, 0);

            var cachingCount = CrossChainConstants.DefaultBlockCacheEntityCount * 2;
            for (int i = 0; i <= cachingCount; i++)
            {
                blockInfoCache.Add(new ParentChainBlockData
                {
                    ChainId = chainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom((i + 1).ToString())
                });
            }

            var list = new List<ParentChainBlockData>();
            for (int i = 0; i < CrossChainConstants.DefaultBlockCacheEntityCount; i++)
            {
                list.Add(new ParentChainBlockData
                {
                    ChainId = chainId,
                    Height = (i + 1),
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom((i + 1).ToString())
                });
            }

            var fakeCache = new Dictionary<int, List<ICrossChainBlockEntity>> {{chainId, blockInfoCache}};
            AddFakeCacheData(fakeCache);
            {
                var crossChainBlockData = FakeCrossChainBlockData(new SideChainBlockData[0], list);
                var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingDataAsync(
                    crossChainBlockData,
                    Hash.Empty, 1);
                list = new List<ParentChainBlockData>();
                for (int i = 0; i < CrossChainConstants.DefaultBlockCacheEntityCount; i++)
                {
                    list.Add(new ParentChainBlockData
                    {
                        ChainId = chainId,
                        Height = (i + 1),
                        TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom((i + 1).ToString())
                    });
                }

                Assert.True(res);
            }
            {
                var crossChainBlockData = FakeCrossChainBlockData(new SideChainBlockData[0], list);
                var res = await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingDataAsync(
                    crossChainBlockData,
                    Hash.Empty, 2);
                Assert.True(res);
            }
        }

        [Fact]
        public void CrossChainRequestExceptionTest()
        {
            var message = "message";
            Should.Throw<CrossChainRequestException>(() => throw new CrossChainRequestException(message));
            Should.Throw<CrossChainRequestException>(() =>
                throw new CrossChainRequestException(message, new Exception()));
        }

        private CrossChainBlockData FakeCrossChainBlockData(IEnumerable<SideChainBlockData> sideChainBlockDataList,
            IEnumerable<ParentChainBlockData> parentChainBlockDataList)
        {
            return new CrossChainBlockData
            {
                SideChainBlockDataList = {sideChainBlockDataList},
                ParentChainBlockDataList = {parentChainBlockDataList}
            };
        }
    }
}