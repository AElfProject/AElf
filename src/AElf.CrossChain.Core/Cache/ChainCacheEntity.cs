using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AElf.CrossChain.Cache
{
    public class ChainCacheEntity
    {
        private BlockingCollection<IBlockCacheEntity> BlockCacheEntities { get; } =
            new BlockingCollection<IBlockCacheEntity>(new ConcurrentQueue<IBlockCacheEntity>());

        private BlockingCollection<IBlockCacheEntity> DequeuedBlockCacheEntities { get; } = new BlockingCollection<IBlockCacheEntity>();

        private readonly int _cachedBoundedCapacity =
            Math.Max(CrossChainConstants.MaximalCountForIndexingSideChainBlock,
                CrossChainConstants.MaximalCountForIndexingParentChainBlock) *
            CrossChainConstants.MinimalBlockCacheEntityCount;
        
        private readonly long _initTargetHeight;
        
        public ChainCacheEntity(long chainHeight)
        {
            _initTargetHeight = chainHeight;
        }

        public long TargetChainHeight()
        {
            var lastEnqueuedBlockCacheEntity = BlockCacheEntities.LastOrDefault();
            if (lastEnqueuedBlockCacheEntity != null)
                return lastEnqueuedBlockCacheEntity.Height + 1;
            var lastDequeuedBlockCacheEntity = DequeuedBlockCacheEntities.LastOrDefault();
            if (lastDequeuedBlockCacheEntity != null) 
                return lastDequeuedBlockCacheEntity.Height + 1;
            return _initTargetHeight;
        }
        
        public bool TryAdd(IBlockCacheEntity blockCacheEntity)
        {
            if (BlockCacheEntities.Count >= _cachedBoundedCapacity)
                return false;
            // thread unsafe in some extreme cases, but it can be covered with caching mechanism.
            if (blockCacheEntity.Height != TargetChainHeight())
                return false;
            var res = BlockCacheEntities.TryAdd(blockCacheEntity);
            return res;
        }
        
        /// <summary>
        /// Try Take element from cached queue.
        /// </summary>
        /// <param name="height">Height of block info needed</param>
        /// <param name="blockCacheEntity"></param>
        /// <param name="isCacheSizeLimited">Use <see cref="_cachedBoundedCapacity"/> as cache count threshold if true.</param>
        /// <returns></returns>
        public bool TryTake(long height, out IBlockCacheEntity blockCacheEntity, bool isCacheSizeLimited)
        {
            // clear outdated data
            var cachedInQueue = DequeueBlockCacheEntitiesBeforeHeight(height);
            // isCacheSizeLimited means minimal caching size limit, so that most nodes have this block.
            var lastQueuedHeight = BlockCacheEntities.LastOrDefault()?.Height ?? 0;
            if (cachedInQueue && !(isCacheSizeLimited && lastQueuedHeight < height + CrossChainConstants.MinimalBlockCacheEntityCount))
            {
                return TryDequeue(out blockCacheEntity);
            }
            
            blockCacheEntity = GetLastDequeuedBlockCacheEntity(height);
            if (blockCacheEntity != null)
                return !isCacheSizeLimited ||
                       BlockCacheEntities.Count + DequeuedBlockCacheEntities.Count(ci => ci.Height >= height) 
                       >= CrossChainConstants.MinimalBlockCacheEntityCount;
            
            return false;
        }

        /// <summary>
        /// Return first element in cached queue.
        /// </summary>
        /// <returns></returns>
        private IBlockCacheEntity GetLastDequeuedBlockCacheEntity(long height)
        {
            return DequeuedBlockCacheEntities.LastOrDefault(c => c.Height == height);
        }
        
        /// <summary>
        /// Cache outdated data. The block with height lower than <paramref name="height"/> is outdated.
        /// </summary>
        /// <param name="height"></param>
        private bool DequeueBlockCacheEntitiesBeforeHeight(long height)
        {
            while (true)
            {
                var blockCacheEntity = BlockCacheEntities.FirstOrDefault();
                if (blockCacheEntity == null || blockCacheEntity.Height > height)
                    return false;
                if (blockCacheEntity.Height == height)
                    return true;
                TryDequeue(out blockCacheEntity);
            }
        }
        
        /// <summary>
        /// Cache block info lately removed.
        /// Dequeue one element if the cached count reaches <see cref="_cachedBoundedCapacity"/>
        /// </summary>
        /// <param name="blockCacheEntity"></param>
        private bool TryDequeue(out IBlockCacheEntity blockCacheEntity)
        {
            var res = BlockCacheEntities.TryTake(out blockCacheEntity,
                CrossChainConstants.WaitingIntervalInMillisecond);
            if (res)
            {
                DequeuedBlockCacheEntities.Add(blockCacheEntity);
                if (DequeuedBlockCacheEntities.Count >= _cachedBoundedCapacity)
                    DequeuedBlockCacheEntities.Take();
            }

            return res;
        }
    }
}