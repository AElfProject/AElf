using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace AElf.CrossChain.Cache
{
    public class CrossChainMemoryCacheTest : CrossChainTestBase
    {
        [Fact]
        public void TryAdd_SingleThread_Success()
        {
            var height = 1;         
            var blockInfoCache = new BlockInfoCache(1);
            var res = blockInfoCache.TryAdd(new SideChainBlockData
            {
                SideChainHeight = height
            });
            Assert.True(res);
            Assert.True(blockInfoCache.TargetChainHeight() == height + 1);
        }
        
        [Fact]
        public void TryAdd_SingleThread_Fail()
        {
            var height = 2;
            var initTarget = 1;
            var blockInfoCache = new BlockInfoCache(initTarget);
            var res = blockInfoCache.TryAdd(new SideChainBlockData
            {
                SideChainHeight = height
            });
            Assert.False(res);
            Assert.True(blockInfoCache.TargetChainHeight() == initTarget);
        }

        [Fact]
        public void TryAdd_Twice_SingleThread_Success()
        {
            var height = 1;         
            var blockInfoCache = new BlockInfoCache(1);
            blockInfoCache.TryAdd(new SideChainBlockData
            {
                SideChainHeight = height++
            });
            var res = blockInfoCache.TryAdd(new SideChainBlockData
            {
                SideChainHeight = height
            });
            Assert.True(res);
            Assert.True(blockInfoCache.TargetChainHeight() == height + 1);
        }

        [Fact]
        public void TryAdd_MultiThreads_WithDifferentData()
        {
            var initTarget = 1;
            var blockInfoCache = new BlockInfoCache(initTarget);
            var i = 0;
            var taskList = new List<Task>();
            while (i < 5)
            {
                var j = i;
                var t = Task.Run(() => blockInfoCache.TryAdd(new SideChainBlockData
                {
                    SideChainHeight = 2 * j + 1
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
            var initTarget = 1;
            var blockInfoCache = new BlockInfoCache(initTarget);
            var i = 0;
            while (i < 5)
            {
                blockInfoCache.TryAdd(new SideChainBlockData
                {
                    SideChainHeight = i++
                });
            }
            Assert.True(blockInfoCache.TargetChainHeight() == 5);
        }

        [Fact]
        public void TryAdd_DataNotContinuous()
        {
            var initTarget = 1;
            var blockInfoCache = new BlockInfoCache(initTarget);
            blockInfoCache.TryAdd(new SideChainBlockData
            {
                SideChainHeight = 1
            });
            blockInfoCache.TryAdd(new SideChainBlockData
            {
                SideChainHeight = 2
            });
            
            // 3 is absent.
            blockInfoCache.TryAdd(new SideChainBlockData
            {
                SideChainHeight = 4
            });
            Assert.True(blockInfoCache.TargetChainHeight() == 3);
        }
        
        [Fact]
        public void TryAdd_MultiThreads_WithSameData()
        {
            var initTarget = 1;
            var blockInfoCache = new BlockInfoCache(initTarget);
            var i = 0;
            var taskList = new List<Task>();
            while (i++ < 5)
            {
                var t = Task.Run(() => blockInfoCache.TryAdd(new SideChainBlockData
                {
                    SideChainHeight = initTarget
                }));
                taskList.Add(t);
            }

            Task.WaitAll(taskList.ToArray());
            Assert.True(blockInfoCache.TargetChainHeight() == initTarget + 1);
        }

        [Fact]
        public void TryTake_WithoutCache()
        {
            var initTarget = 1;
            var blockInfoCache = new BlockInfoCache(initTarget);
            var res = blockInfoCache.TryTake(initTarget, out var blockInfo, false);
            Assert.False(res);
        }
        
        [Fact]
        public void TryTake_WithoutEnoughCache()
        {
            var initTarget = 1;
            var blockInfoCache = new BlockInfoCache(initTarget);
            int i = 0;
            while (i++ < CrossChainConstants.MinimalBlockInfoCacheThreshold)
            {
                var t = blockInfoCache.TryAdd(new SideChainBlockData
                {
                    SideChainHeight = i
                });
            }
            var res = blockInfoCache.TryTake(initTarget, out var blockInfo, true);
            Assert.False(res);
        }
        
        [Fact]
        public void TryTake_WithSizeLimit()
        {
            var initTarget = 1;
            var blockInfoCache = new BlockInfoCache(initTarget);
            int i = 0;
            while (i++ <= CrossChainConstants.MinimalBlockInfoCacheThreshold)
            {
                var t = blockInfoCache.TryAdd(new SideChainBlockData
                {
                    SideChainHeight = i
                });
            }
            var res = blockInfoCache.TryTake(initTarget, out var blockInfo, true);
            Assert.True(res);
            Assert.True(blockInfo.Height == initTarget);
        }
        
        [Fact]
        public void TryTake_WithoutSizeLimit()
        {
            var initTarget = 1;
            var blockInfoCache = new BlockInfoCache(initTarget);
            blockInfoCache.TryAdd(new SideChainBlockData
            {
                SideChainHeight = 1
            });
            var res = blockInfoCache.TryTake(initTarget, out var blockInfo, false);
            Assert.True(res);
            Assert.True(blockInfo.Height == initTarget);
        }

        [Fact]
        public void TryTake_WithClearCacheNeeded()
        {
            var initTarget = 2;
            var blockInfoCache = new BlockInfoCache(initTarget);
            int i = 0;
            while (i++ < (int)initTarget +  CrossChainConstants.MinimalBlockInfoCacheThreshold)
            {
                var t = blockInfoCache.TryAdd(new SideChainBlockData
                {
                    SideChainHeight = i
                });
            }
            var res = blockInfoCache.TryTake(initTarget, out var blockInfo, true);
            Assert.True(res);
            Assert.True(blockInfo.Height == initTarget);
        }

        [Fact]
        public void TryTake_Twice()
        {
            var initTarget = 2;
            var blockInfoCache = new BlockInfoCache(initTarget);
            int i = 0;
            while (i++ < (int)initTarget +  CrossChainConstants.MinimalBlockInfoCacheThreshold)
            {
                var t = blockInfoCache.TryAdd(new SideChainBlockData
                {
                    SideChainHeight = i
                });
            }
            blockInfoCache.TryTake(initTarget, out var b1, true);
            var res = blockInfoCache.TryTake(initTarget, out var b2, true);
            Assert.True(res);
            Assert.Equal(b1, b2);
        }

        [Fact]
        public void TryTake_OutDatedData()
        {
            var initTarget = 1;
            var blockInfoCache = new BlockInfoCache(initTarget);
            int i = 0;
            while (i++ < (int)initTarget +  CrossChainConstants.MinimalBlockInfoCacheThreshold)
            {
                var t = blockInfoCache.TryAdd(new SideChainBlockData
                {
                    SideChainHeight = i
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
            var initTarget = 1;
            var blockInfoCache = new BlockInfoCache(initTarget);
            blockInfoCache.TryAdd(new SideChainBlockData
            {
                SideChainHeight = 1
            });
            blockInfoCache.TryAdd(new SideChainBlockData
            {
                SideChainHeight = 2
            });
            blockInfoCache.TryTake(1, out _, false);
            blockInfoCache.TryTake(2, out _, false);

            Assert.True(blockInfoCache.TargetChainHeight() == 3);
        }
    }
}