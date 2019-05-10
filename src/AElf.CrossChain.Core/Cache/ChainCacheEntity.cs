using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AElf.CrossChain.Cache
{
    public class ChainCacheEntity
    {
        private BlockingCollection<BlockCacheEntity> ToBeIndexedBlockInfoQueue { get; } =
            new BlockingCollection<BlockCacheEntity>(new ConcurrentQueue<BlockCacheEntity>());

        private Queue<BlockCacheEntity> CachedIndexedBlockInfoQueue { get; } = new Queue<BlockCacheEntity>();

        private readonly int _cachedBoundedCapacity =
            Math.Max(CrossChainConstants.MaximalCountForIndexingSideChainBlock,
                CrossChainConstants.MaximalCountForIndexingParentChainBlock) *
            CrossChainConstants.MinimalBlockInfoCacheThreshold;
        
        private readonly long _initTargetHeight;
        
        public ChainCacheEntity(long chainHeight)
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
        
        public bool TryAdd(BlockCacheEntity blockCacheEntity)
        {
            if (ToBeIndexedBlockInfoQueue.Count() >= _cachedBoundedCapacity)
                return false;
            // thread unsafe in some extreme cases, but it can be covered with caching mechanism.
            if (blockCacheEntity.Height != TargetChainHeight())
                return false;
            var res = ToBeIndexedBlockInfoQueue.TryAdd(blockCacheEntity);
            return res;
        }
        
        /// <summary>
        /// Try Take element from cached queue.
        /// </summary>
        /// <param name="height">Height of block info needed</param>
        /// <param name="blockCacheEntity"></param>
        /// <param name="isCacheSizeLimited">Use <see cref="_cachedBoundedCapacity"/> as cache count threshold if true.</param>
        /// <returns></returns>
        public bool TryTake(long height, out BlockCacheEntity blockCacheEntity, bool isCacheSizeLimited)
        {
            // clear outdated data
            var cachedInQueue = CacheBlockInfoBeforeHeight(height);
            // isCacheSizeLimited means minimal caching size limit, so that most nodes have this block.
            var lastQueuedHeight = ToBeIndexedBlockInfoQueue.LastOrDefault()?.Height ?? 0;
            if (cachedInQueue && !(isCacheSizeLimited && lastQueuedHeight < height + CrossChainConstants.MinimalBlockInfoCacheThreshold))
            {
                var res = ToBeIndexedBlockInfoQueue.TryTake(out blockCacheEntity, 
                    CrossChainConstants.WaitingIntervalInMillisecond);
                if(res)
                    CacheBlockInfo(blockCacheEntity);
                return res;
            }
            
            blockCacheEntity = LastBlockInfoInCache(height);
            if (blockCacheEntity != null)
                return !isCacheSizeLimited ||
                       ToBeIndexedBlockInfoQueue.Count + CachedIndexedBlockInfoQueue.Count(ci => ci.Height >= height) 
                       >= CrossChainConstants.MinimalBlockInfoCacheThreshold;
            
            return false;
        }

        /// <summary>
        /// Return first element in cached queue.
        /// </summary>
        /// <returns></returns>
        private BlockCacheEntity LastBlockInfoInCache(long height)
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
                var res = ToBeIndexedBlockInfoQueue.TryTake(out blockInfo, CrossChainConstants.WaitingIntervalInMillisecond);
                if (res)
                    CacheBlockInfo(blockInfo);
            }
        }
        
        /// <summary>
        /// Cache block info lately removed.
        /// Dequeue one element if the cached count reaches <see cref="_cachedBoundedCapacity"/>
        /// </summary>
        /// <param name="blockCacheEntity"></param>
        private void CacheBlockInfo(BlockCacheEntity blockCacheEntity)
        {
            CachedIndexedBlockInfoQueue.Enqueue(blockCacheEntity);
            if (CachedIndexedBlockInfoQueue.Count <= _cachedBoundedCapacity)
                return;
            CachedIndexedBlockInfoQueue.Dequeue();
        }
    }
}