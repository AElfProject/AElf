using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AElf.Kernel.SmartContract.Application;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public interface ICalculateFunctionCacheProvider
    {
        Dictionary<int, ICalculateWay> GetPieceWiseFunctionFromNormalCache();
        bool TryGetPieceWiseFunctionFromForkCacheByBlockIndex(BlockIndex index, out Dictionary<int, ICalculateWay> value);
        void SetPieceWiseFunctionToNormalCache(Dictionary<int, ICalculateWay> cache);
        void SetPieceWiseFunctionToForkCache(BlockIndex blockIndex, Dictionary<int, ICalculateWay> funcDic);
        void RemoveFromForkCacheByBlockIndex(List<BlockIndex> blockIndexes);
        void SyncCache(List<BlockIndex> blockIndexes);
        BlockIndex[] GetForkCacheKeys();
    }

    public class CalculateFunctionCacheProvider : ICalculateFunctionCacheProvider
    {
        private Dictionary<int, ICalculateWay> _pieceWiseFuncCache;

        private readonly ConcurrentDictionary<BlockIndex, Dictionary<int, ICalculateWay>> _forkCache;

        public CalculateFunctionCacheProvider()
        {
            _forkCache = new ConcurrentDictionary<BlockIndex, Dictionary<int, ICalculateWay>>();
        }

        public Dictionary<int, ICalculateWay> GetPieceWiseFunctionFromNormalCache()
        {
            return _pieceWiseFuncCache;
        }

        public bool TryGetPieceWiseFunctionFromForkCacheByBlockIndex(BlockIndex index, out Dictionary<int, ICalculateWay> value)
        {
            return _forkCache.TryGetValue(index, out value);
        }

        public void SetPieceWiseFunctionToNormalCache(Dictionary<int, ICalculateWay> cache)
        {
            _pieceWiseFuncCache = cache;
        }

        public void SetPieceWiseFunctionToForkCache(BlockIndex blockIndex, Dictionary<int, ICalculateWay> funcDic)
        {
            _forkCache[blockIndex] = funcDic;
        }
        public void RemoveFromForkCacheByBlockIndex(List<BlockIndex> blockIndexes)
        {
            foreach (var blockIndex in blockIndexes.Where(blockIndex => _forkCache.TryGetValue(blockIndex, out _)))
            {
                _forkCache.TryRemove(blockIndex, out _);
            }
        }

        public void SyncCache(List<BlockIndex> blockIndexes)
        {
            foreach (var blockIndex in blockIndexes)
            {
                if (!_forkCache.TryGetValue(blockIndex, out var calAlgorithm)) continue;
                _pieceWiseFuncCache = calAlgorithm;
                _forkCache.TryRemove(blockIndex, out _);
            }
        }
        public BlockIndex[] GetForkCacheKeys()
        {
            return  _forkCache.Keys.ToArray();
        }
    }
}