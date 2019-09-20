using System.Collections.Generic;
using System.Threading.Tasks;
using Acs7;
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
                Height = height
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
                Height = height++
            });
            var res = blockInfoCache.TryAdd(new SideChainBlockData
            {
                Height = height
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
                    Height = 2 * j + 1
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
                    Height = i++
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
                Height = 1
            });
            blockInfoCache.TryAdd(new SideChainBlockData
            {
                Height = 2
            });
            
            // 3 is absent.
            blockInfoCache.TryAdd(new SideChainBlockData
            {
                Height = 4
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
                    Height = initTarget
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
            var res = blockInfoCache.TryTake(initTarget, out var blockInfo, false);
            Assert.False(res);
        }
        
        [Fact]
        public void TryTake_WithoutEnoughCache()
        {
            var chainId = 123;
            var initTarget = 1;
            var blockInfoCache = new ChainCacheEntity(chainId, initTarget);
            int i = 0;
            while (i++ < CrossChainConstants.MinimalBlockCacheEntityCount)
            {
                var t = blockInfoCache.TryAdd(new SideChainBlockData
                {
                    Height = i
                });
            }
            
            var res = blockInfoCache.TryTake(initTarget, out var blockInfo, true);
            Assert.False(res);
        }
        
        [Fact]
        public void TryTake_WithSizeLimit()
        {
            var chainId = 123;
            var initTarget = 1;
            var blockInfoCache = new ChainCacheEntity(chainId, initTarget);
            int i = 0;
            while (i++ <= CrossChainConstants.MinimalBlockCacheEntityCount)
            {
                var t = blockInfoCache.TryAdd(new SideChainBlockData
                {
                    Height = i
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
                Height = 1
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
            while (i++ < (int)initTarget +  CrossChainConstants.MinimalBlockCacheEntityCount)
            {
                var t = blockInfoCache.TryAdd(new SideChainBlockData
                {
                    Height = i
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
            while (i++ < initTarget + CrossChainConstants.MinimalBlockCacheEntityCount)
            {
                var t = blockInfoCache.TryAdd(new SideChainBlockData
                {
                    Height = i
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
            while (i++ < initTarget + CrossChainConstants.MinimalBlockCacheEntityCount)
            {
                var t = blockInfoCache.TryAdd(new SideChainBlockData
                {
                    Height = i
                });
            }
            
            blockInfoCache.TryTake(2, out var b1, true);
            var res = blockInfoCache.TryTake(1, out var b2, true);
            Assert.True(res);
            Assert.True(b2.Height == 1);
        }

        [Fact]
        public void TargetHeight_WithEmptyQueue()
        {
            var chainId = 123;
            var initTarget = 1;
            var blockInfoCache = new ChainCacheEntity(chainId, initTarget);
            blockInfoCache.TryAdd(new SideChainBlockData
            {
                Height = 1
            });
            blockInfoCache.TryAdd(new SideChainBlockData
            {
                Height = 2
            });
            blockInfoCache.TryTake(1, out _, false);
            blockInfoCache.TryTake(2, out _, false);

            Assert.Equal(3, blockInfoCache.TargetChainHeight());
        }
    }
}