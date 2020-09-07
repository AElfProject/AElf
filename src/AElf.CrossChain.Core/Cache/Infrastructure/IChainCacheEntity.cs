using System.Collections.Concurrent;
using System.Linq;

namespace AElf.CrossChain.Cache.Infrastructure
{
    public interface IChainCacheEntity
    {
        bool TryAdd(ICrossChainBlockEntity crossChainBlockEntity);
        long TargetChainHeight();
        bool TryTake(long height, out ICrossChainBlockEntity crossChainBlockEntity, bool isCacheSizeLimited);
        void ClearOutOfDateCacheByHeight(long height);
    }

    public class ChainCacheEntity : IChainCacheEntity
    {
        private readonly ConcurrentDictionary<long, ICrossChainBlockEntity> _cache =
            new ConcurrentDictionary<long, ICrossChainBlockEntity>();

        private long _targetHeight;
        private readonly int _chainId;

        public ChainCacheEntity(int chainId, long chainHeight)
        {
            _chainId = chainId;
            _targetHeight = chainHeight;
        }

        public long TargetChainHeight()
        {
            return _cache.Count < CrossChainConstants.ChainCacheEntityCapacity ? _targetHeight : -1;
        }

        public bool TryAdd(ICrossChainBlockEntity crossChainBlockEntity)
        {
            if (crossChainBlockEntity.Height != TargetChainHeight())
                return false;
            var res = ValidateBlockCacheEntity(crossChainBlockEntity) &&
                      _cache.TryAdd(crossChainBlockEntity.Height, crossChainBlockEntity);
            if (res)
                _targetHeight = crossChainBlockEntity.Height + 1;
            return res;
        }

        /// <summary>
        /// Try take element from cached queue.
        /// </summary>
        /// <param name="height">Height of block info needed</param>
        /// <param name="crossChainBlockEntity"></param>
        /// <param name="isCacheSizeLimited">Use <see cref="CrossChainConstants.ChainCacheEntityCapacity"/> as cache count threshold if true.</param>
        /// <returns></returns>
        public bool TryTake(long height, out ICrossChainBlockEntity crossChainBlockEntity, bool isCacheSizeLimited)
        {
            // clear outdated data
            if (!_cache.TryGetValue(height, out crossChainBlockEntity))
            {
                return false;
            }

            var lastQueuedHeight = _targetHeight - 1;
            return !isCacheSizeLimited || lastQueuedHeight >= height + CrossChainConstants.DefaultBlockCacheEntityCount;
        }

        public void ClearOutOfDateCacheByHeight(long height)
        {
            foreach (var h in _cache.Keys.Where(k => k <= height))
            {
                _cache.TryRemove(h, out _);
            }

            if (_cache.Count == 0)
            {
                _targetHeight = height + 1;
            }
        }

        private bool ValidateBlockCacheEntity(ICrossChainBlockEntity crossChainBlockEntity)
        {
            return crossChainBlockEntity.Height >= AElfConstants.GenesisBlockHeight && 
                   crossChainBlockEntity.ChainId == _chainId &&
                   crossChainBlockEntity.TransactionStatusMerkleTreeRoot != null;
        }
    }
}