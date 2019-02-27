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

        private static readonly int CachedBoundedCapacity =
            Math.Max(CrossChainConsts.MaximalCountForIndexingSideChainBlock,
                CrossChainConsts.MaximalCountForIndexingParentChainBlock) *
            CrossChainConsts.MinimalBlockInfoCacheThreshold;
        private readonly ulong _initTargetHeight;
        public ulong TargetChainHeight
        {
            get
            {
                var lastQueuedHeight = LastOneHeightInQueue();
                if (lastQueuedHeight != 0)
                    return lastQueuedHeight + 1;
                var lastCachedHeight = LastOneInCache()?.Height ?? 0;
                if (lastCachedHeight != 0)
                    return lastCachedHeight + 1;
                return _initTargetHeight;
            }
        }

        public BlockInfoCache(ulong chainHeight)
        {
            _initTargetHeight = chainHeight;
        }

        public bool TryAdd(IBlockInfo blockInfo)
        {
            // thread unsafe in some extreme cases, but it can be covered with caching mechanism.
            if (blockInfo.Height != TargetChainHeight)
                return false;
            var res = ToBeIndexedBlockInfoQueue.TryAdd(blockInfo);
            return res;
        }
        
        /// <summary>
        /// Try Take element from cached queue.
        /// </summary>
        /// <param name="height">Height of block info needed</param>
        /// <param name="blockInfo"></param>
        /// <param name="isCacheSizeLimited">Use <see cref="CachedBoundedCapacity"/> as cache count threshold if true.</param>
        /// <returns></returns>
        public bool TryTake(ulong height, out IBlockInfo blockInfo, bool isCacheSizeLimited)
        {
            // clear outdated data
            var cachedInQueue = CacheBlockInfoBeforeHeight(height);
            // isCacheSizeLimited means minimal caching size , for most nodes have this block.
            
            if (cachedInQueue && !(isCacheSizeLimited && LastOneHeightInQueue() < height + (ulong) CrossChainConsts.MinimalBlockInfoCacheThreshold))
            {
                var res = ToBeIndexedBlockInfoQueue.TryTake(out blockInfo, 
                    CrossChainConsts.WaitingIntervalInMillisecond);
                if(res)
                    CacheBlockInfo(blockInfo);
                return res;
            }
            
            // this is because of rollback 
            blockInfo = LastOneInCache(height);
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
        private IBlockInfo LastOneInCache(ulong height = 0)
        {
            return height == 0 ? CachedIndexedBlockInfoQueue.FirstOrDefault() 
                : CachedIndexedBlockInfoQueue.FirstOrDefault(c => c.Height == height);
        }

        private ulong LastOneHeightInQueue()
        {
            return ToBeIndexedBlockInfoQueue.LastOrDefault()?.Height ?? 0;
        }
        
        /// <summary>
        /// Cache outdated data. The block with height lower than <paramref name="height"/> is outdated.
        /// </summary>
        /// <param name="height"></param>
        private bool CacheBlockInfoBeforeHeight(ulong height)
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
        /// Dequeue one element if the cached count reaches <see cref="CachedBoundedCapacity"/>
        /// </summary>                                                   
        /// <param name="blockInfo"></param>
        private void CacheBlockInfo(IBlockInfo blockInfo)
        {
            CachedIndexedBlockInfoQueue.Enqueue(blockInfo);
            if (CachedIndexedBlockInfoQueue.Count <= CachedBoundedCapacity)
                return;
            CachedIndexedBlockInfoQueue.Dequeue();
        }
    }
}