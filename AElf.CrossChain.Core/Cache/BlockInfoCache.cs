using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AElf.CrossChain.Cache
{
    public class BlockInfoCache
    {
        private BlockingCollection<IBlockInfo> ToBeIndexedBlockInfoQueue { get; } =
            new BlockingCollection<IBlockInfo>(new ConcurrentQueue<IBlockInfo>());

        private Queue<IBlockInfo> CachedIndexedBlockInfoQueue { get; } = new Queue<IBlockInfo>();

        private readonly int _cachedBoundedCapacity =
            Math.Max(CrossChainConsts.MaximalCountForIndexingSideChainBlock,
                CrossChainConsts.MaximalCountForIndexingParentChainBlock) *
            CrossChainConsts.MinimalBlockInfoCacheThreshold;
        private readonly long _initTargetHeight;
        
        public BlockInfoCache(long chainHeight)
        {
            _initTargetHeight = chainHeight;
        }

        public long TargetChainHeight()
        {
            var lastQueuedBlockInfo = ToBeIndexedBlockInfoQueue.LastOrDefault();
            if (lastQueuedBlockInfo != null)
                return lastQueuedBlockInfo.Height + 1;
            var lastCachedBlockInfo = CachedIndexedBlockInfoQueue.LastOrDefault();
            if (lastCachedBlockInfo != null) 
                return lastCachedBlockInfo.Height + 1;
            return _initTargetHeight;
        }
        
        public bool TryAdd(IBlockInfo blockInfo)
        {
            // thread unsafe in some extreme cases, but it can be covered with caching mechanism.
            if (blockInfo.Height != TargetChainHeight())
                return false;
            var res = ToBeIndexedBlockInfoQueue.TryAdd(blockInfo);
            return res;
        }
        
        /// <summary>
        /// Try Take element from cached queue.
        /// </summary>
        /// <param name="height">Height of block info needed</param>
        /// <param name="blockInfo"></param>
        /// <param name="isCacheSizeLimited">Use <see cref="_cachedBoundedCapacity"/> as cache count threshold if true.</param>
        /// <returns></returns>
        public bool TryTake(long height, out IBlockInfo blockInfo, bool isCacheSizeLimited)
        {
            // clear outdated data
            var cachedInQueue = CacheBlockInfoBeforeHeight(height);
            // isCacheSizeLimited means minimal caching size limit, so that most nodes have this block.
            var lastQueuedHeight = ToBeIndexedBlockInfoQueue.LastOrDefault()?.Height ?? 0;
            if (cachedInQueue && !(isCacheSizeLimited && lastQueuedHeight < height + CrossChainConsts.MinimalBlockInfoCacheThreshold))
            {
                var res = ToBeIndexedBlockInfoQueue.TryTake(out blockInfo, 
                    CrossChainConsts.WaitingIntervalInMillisecond);
                if(res)
                    CacheBlockInfo(blockInfo);
                return res;
            }
            
            blockInfo = LastBlockInfoInCache(height);
            if (blockInfo != null)
                return !isCacheSizeLimited ||
                       ToBeIndexedBlockInfoQueue.Count + CachedIndexedBlockInfoQueue.Count(ci => ci.Height >= height) 
                       >= CrossChainConsts.MinimalBlockInfoCacheThreshold;
            
            return false;
        }

        /// <summary>
        /// Return first element in cached queue.
        /// </summary>
        /// <returns></returns>
        private IBlockInfo LastBlockInfoInCache(long height)
        {
            return CachedIndexedBlockInfoQueue.LastOrDefault(c => c.Height == height);
        }
        
        /// <summary>
        /// Cache outdated data. The block with height lower than <paramref name="height"/> is outdated.
        /// </summary>
        /// <param name="height"></param>
        private bool CacheBlockInfoBeforeHeight(long height)
        {
            while (true)
            {
                var blockInfo = ToBeIndexedBlockInfoQueue.FirstOrDefault();
                if (blockInfo == null || blockInfo.Height > height)
                    return false;
                if (blockInfo.Height == height)
                    return true;
                var res = ToBeIndexedBlockInfoQueue.TryTake(out blockInfo, CrossChainConsts.WaitingIntervalInMillisecond);
                if (res)
                    CacheBlockInfo(blockInfo);
            }
        }
        
        /// <summary>
        /// Cache block info lately removed.
        /// Dequeue one element if the cached count reaches <see cref="_cachedBoundedCapacity"/>
        /// </summary>                                                   
        /// <param name="blockInfo"></param>
        private void CacheBlockInfo(IBlockInfo blockInfo)
        {
            CachedIndexedBlockInfoQueue.Enqueue(blockInfo);
            if (CachedIndexedBlockInfoQueue.Count <= _cachedBoundedCapacity)
                return;
            CachedIndexedBlockInfoQueue.Dequeue();
        }
    }
}