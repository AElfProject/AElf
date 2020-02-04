using System.Collections.Concurrent;

namespace AElf.CrossChain.Cache
{
    public interface IChainCacheEntity
    {
        bool TryAdd(IBlockCacheEntity blockCacheEntity);
        long TargetChainHeight();
        bool TryTake(long height, out IBlockCacheEntity blockCacheEntity, bool isCacheSizeLimited);
        void ClearOutOfDateCacheByHeight(long height);
    }

    public class ChainCacheEntity : IChainCacheEntity
    {
        private readonly ConcurrentDictionary<long, IBlockCacheEntity> _cache =
            new ConcurrentDictionary<long, IBlockCacheEntity>();

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

        public bool TryAdd(IBlockCacheEntity blockCacheEntity)
        {
            if (blockCacheEntity.Height != TargetChainHeight())
                return false;
            var res = ValidateBlockCacheEntity(blockCacheEntity) &&
                      _cache.TryAdd(blockCacheEntity.Height, blockCacheEntity);
            if (res)
                _targetHeight = blockCacheEntity.Height + 1;
            return res;
        }

        /// <summary>
        /// Try take element from cached queue.
        /// </summary>
        /// <param name="height">Height of block info needed</param>
        /// <param name="blockCacheEntity"></param>
        /// <param name="isCacheSizeLimited">Use <see cref="CrossChainConstants.ChainCacheEntityCapacity"/> as cache count threshold if true.</param>
        /// <returns></returns>
        public bool TryTake(long height, out IBlockCacheEntity blockCacheEntity, bool isCacheSizeLimited)
        {
            // clear outdated data
            var cachedInQueue = _cache.TryGetValue(height, out var cachedData);
            blockCacheEntity = cachedData;
            if (!cachedInQueue)
                return false;

            var lastQueuedHeight = _targetHeight - 1;
            return !isCacheSizeLimited || lastQueuedHeight >= height + CrossChainConstants.DefaultBlockCacheEntityCount;
        }

        public void ClearOutOfDateCacheByHeight(long height)
        {
            while (true)
            {
                if (!_cache.TryRemove(height--, out _))
                    return;
            }
        }

        private bool ValidateBlockCacheEntity(IBlockCacheEntity blockCacheEntity)
        {
            return blockCacheEntity.Height >= Constants.GenesisBlockHeight && blockCacheEntity.ChainId == _chainId &&
                   blockCacheEntity.TransactionStatusMerkleTreeRoot != null;
        }
    }
}