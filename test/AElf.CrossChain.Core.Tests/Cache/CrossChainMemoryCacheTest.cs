using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Standards.ACS7;
using AElf.CrossChain.Cache.Infrastructure;
using AElf.Types;
using Xunit;

namespace AElf.CrossChain.Cache
{
    public class CrossChainMemoryCacheTest : CrossChainTestBase
    {
        [Fact]
        public void TryAdd_SingleThread_Success()
        {
            var chainId = 123;
            var height = 1;
            var blockInfoCache = new ChainCacheEntity(chainId, 1);
            var res = blockInfoCache.TryAdd(new SideChainBlockData
            {
                Height = height,
                ChainId = chainId,
                TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom(height.ToString())
            });
            Assert.True(res);
            Assert.True(blockInfoCache.TargetChainHeight() == height + 1);
        }

        [Fact]
        public void TryAdd_SingleThread_Fail()
        {
            var height = 2;
            var initTarget = 1;
            var chainId = 123;
            var blockInfoCache = new ChainCacheEntity(chainId, initTarget);
            var res = blockInfoCache.TryAdd(new SideChainBlockData
            {
                Height = height
            });
            Assert.False(res);
            Assert.True(blockInfoCache.TargetChainHeight() == initTarget);
        }

        [Fact]
        public void TryAdd_Twice_SingleThread_Success()
        {
            var chainId = 123;
            var height = 1;
            var blockInfoCache = new ChainCacheEntity(chainId, 1);
            blockInfoCache.TryAdd(new SideChainBlockData
            {
                Height = height,
                ChainId = chainId,
                TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom(height++.ToString())
            });
            var res = blockInfoCache.TryAdd(new SideChainBlockData
            {
                Height = height,
                ChainId = chainId,
                TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom(height.ToString())
            });
            Assert.True(res);
            Assert.True(blockInfoCache.TargetChainHeight() == height + 1);
        }

        [Fact]
        public void TryAdd_MultiThreads_WithDifferentData()
        {
            var chainId = 123;
            var initTarget = 1;
            var blockInfoCache = new ChainCacheEntity(chainId, initTarget);
            var i = 0;
            var taskList = new List<Task>();
            while (i < 5)
            {
                var j = i;
                var t = Task.Run(() => blockInfoCache.TryAdd(new SideChainBlockData
                {
                    Height = 2 * j + 1,
                    ChainId = chainId,
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom((2 * j + 1).ToString())
                }));
                taskList.Add(t);
                i++;
            }

            Task.WaitAll(taskList.ToArray());
            Assert.True(blockInfoCache.TargetChainHeight() == initTarget + 1);
        }

        [Fact]
        public void TryAdd_DataContinuous()
        {
            var chainId = 123;
            var initTarget = 1;
            var blockInfoCache = new ChainCacheEntity(chainId, initTarget);
            var i = 0;
            while (i < 5)
            {
                blockInfoCache.TryAdd(new SideChainBlockData
                {
                    Height = i,
                    ChainId = chainId,
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom(i++.ToString())
                });
            }

            Assert.True(blockInfoCache.TargetChainHeight() == 5);
        }

        [Fact]
        public void TryAdd_DataNotContinuous()
        {
            var chainId = 123;
            var initTarget = 1;
            var blockInfoCache = new ChainCacheEntity(chainId, initTarget);
            blockInfoCache.TryAdd(new SideChainBlockData
            {
                Height = 1,
                ChainId = chainId,
                TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom("1")
            });
            blockInfoCache.TryAdd(new SideChainBlockData
            {
                Height = 2,
                ChainId = chainId,
                TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom("2")
            });

            // 3 is absent.
            blockInfoCache.TryAdd(new SideChainBlockData
            {
                Height = 4,
                ChainId = chainId,
                TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom("4")
            });
            Assert.True(blockInfoCache.TargetChainHeight() == 3);
        }

        [Fact]
        public void TryAdd_MultiThreads_WithSameData()
        {
            var chainId = 123;
            var initTarget = 1;
            var blockInfoCache = new ChainCacheEntity(chainId, initTarget);
            var i = 0;
            var taskList = new List<Task>();
            while (i++ < 5)
            {
                var t = Task.Run(() => blockInfoCache.TryAdd(new SideChainBlockData
                {
                    Height = initTarget,
                    ChainId = chainId,
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom(initTarget.ToString())
                }));
                taskList.Add(t);
            }

            Task.WaitAll(taskList.ToArray());
            Assert.True(blockInfoCache.TargetChainHeight() == initTarget + 1);
        }

        [Fact]
        public void TryTake_WithoutCache()
        {
            var chainId = 123;
            var initTarget = 1;
            var blockInfoCache = new ChainCacheEntity(chainId, initTarget);
            var res = blockInfoCache.TryTake(initTarget, out _, false);
            Assert.False(res);
        }

        [Fact]
        public void TryTake_WithoutEnoughCache()
        {
            var chainId = 123;
            var initTarget = 1;
            var blockInfoCache = new ChainCacheEntity(chainId, initTarget);
            int i = 0;
            while (i++ < CrossChainConstants.DefaultBlockCacheEntityCount)
            {
                blockInfoCache.TryAdd(new SideChainBlockData
                {
                    Height = i
                });
            }

            var res = blockInfoCache.TryTake(initTarget, out _, true);
            Assert.False(res);
        }

        [Fact]
        public void TryTake_WithSizeLimit()
        {
            var chainId = 123;
            var initTarget = 1;
            var blockInfoCache = new ChainCacheEntity(chainId, initTarget);
            int i = 0;
            while (i++ <= CrossChainConstants.DefaultBlockCacheEntityCount)
            {
                blockInfoCache.TryAdd(new SideChainBlockData
                {
                    Height = i,
                    ChainId = chainId,
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom(i.ToString())
                });
            }

            var res = blockInfoCache.TryTake(initTarget, out var blockInfo, true);
            Assert.True(res);
            Assert.True(blockInfo.Height == initTarget);
        }

        [Fact]
        public void TryTake_WithoutSizeLimit()
        {
            var chainId = 123;
            var initTarget = 1;
            var blockInfoCache = new ChainCacheEntity(chainId, initTarget);
            blockInfoCache.TryAdd(new SideChainBlockData
            {
                Height = 1,
                ChainId = chainId,
                TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom("1")
            });
            var res = blockInfoCache.TryTake(initTarget, out var blockInfo, false);
            Assert.True(res);
            Assert.True(blockInfo.Height == initTarget);
        }

        [Fact]
        public void TryTake_WithClearCacheNeeded()
        {
            var chainId = 123;
            var initTarget = 2;
            var blockInfoCache = new ChainCacheEntity(chainId, initTarget);
            int i = 0;
            while (i++ < initTarget + CrossChainConstants.DefaultBlockCacheEntityCount)
            {
                blockInfoCache.TryAdd(new SideChainBlockData
                {
                    ChainId = chainId,
                    Height = i,
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom(i.ToString())
                });
            }

            var res = blockInfoCache.TryTake(initTarget, out var blockInfo, true);
            Assert.True(res);
            Assert.True(blockInfo.Height == initTarget);
        }

        [Fact]
        public void TryTake_Twice()
        {
            var chainId = 123;
            var initTarget = 2;
            var blockInfoCache = new ChainCacheEntity(chainId, initTarget);
            int i = 0;
            while (i++ < initTarget + CrossChainConstants.DefaultBlockCacheEntityCount)
            {
                blockInfoCache.TryAdd(new SideChainBlockData
                {
                    Height = i,
                    ChainId = chainId,
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom(i.ToString())
                });
            }

            var res = blockInfoCache.TryTake(initTarget, out var b1, true);
            Assert.True(res);
            res = blockInfoCache.TryTake(initTarget, out var b2, true);
            Assert.True(res);
            Assert.Equal(b1, b2);
        }

        [Fact]
        public void TryTake_OutDatedData()
        {
            var chainId = 123;
            var initTarget = 1;
            var blockInfoCache = new ChainCacheEntity(chainId, initTarget);
            int i = 0;
            while (i++ < initTarget + CrossChainConstants.DefaultBlockCacheEntityCount)
            {
                blockInfoCache.TryAdd(new SideChainBlockData
                {
                    Height = i,
                    ChainId = chainId,
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom(i.ToString())
                });
            }

            blockInfoCache.TryTake(2, out _, true);
            var res = blockInfoCache.TryTake(1, out var b2, true);
            Assert.True(res);
            Assert.True(b2.Height == 1);
        }

        [Fact]
        public void TargetHeight_WithEmptyQueue()
        {
            var sideChainId = 123;
            var initTarget = 1;
            var blockInfoCache = new ChainCacheEntity(sideChainId, initTarget);
            blockInfoCache.TryAdd(new SideChainBlockData
            {
                Height = 1,
                ChainId = sideChainId,
                TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom("1")
            });
            blockInfoCache.TryAdd(new SideChainBlockData
            {
                Height = 2,
                ChainId = sideChainId,
                TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom("2")
            });
            blockInfoCache.TryTake(1, out _, false);
            blockInfoCache.TryTake(2, out _, false);

            Assert.Equal(3, blockInfoCache.TargetChainHeight());
        }

        [Fact]
        public void ClearByHeight()
        {
            var chainId = 123;
            var initTarget = 2;
            var blockInfoCache = new ChainCacheEntity(chainId, initTarget);
            int i = 0;
            while (i++ < initTarget + CrossChainConstants.DefaultBlockCacheEntityCount)
            {
                blockInfoCache.TryAdd(new SideChainBlockData
                {
                    Height = i,
                    ChainId = chainId,
                    TransactionStatusMerkleTreeRoot = HashHelper.ComputeFrom(i.ToString())
                });
            }
            
            {
                blockInfoCache.ClearOutOfDateCacheByHeight(initTarget - 1);
                var res = blockInfoCache.TryTake(initTarget, out _, false);
                Assert.True(res);
            }
            
            {
                blockInfoCache.ClearOutOfDateCacheByHeight(initTarget);
                var res = blockInfoCache.TryTake(initTarget, out _, false);
                Assert.False(res);
            }

            {
                var targetHeight = blockInfoCache.TargetChainHeight();
                blockInfoCache.ClearOutOfDateCacheByHeight(targetHeight);
                Assert.True(targetHeight + 1 == blockInfoCache.TargetChainHeight());
                for (int j = 0; j < targetHeight; j++)
                {
                    var res = blockInfoCache.TryTake(j, out _, false);
                    Assert.False(res);
                }
            }
        }
    }
}