using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Blockchain.Infrastructure
{
    public interface ICachedBlockProvider
    {
        void AddBlock(Block block);

        void RemoveBlock(Hash blockHash);

        void RemoveBlockUnderHeight(long blockHeight);
        
        CachedBlock GetBlock(Hash blockHash);

        List<CachedBlock> GetBlocks();

        void SetLastIrreversible(Hash blockHash);
    }

    public class CachedBlockProvider : ICachedBlockProvider, ISingletonDependency
    {
        private readonly ConcurrentDictionary<Hash, CachedBlock> _cachedBlocks =
            new ConcurrentDictionary<Hash, CachedBlock>();

        public void AddBlock(Block block)
        {
            if (_cachedBlocks.TryGetValue(block.GetHash(), out _)) return;
            _cachedBlocks[block.GetHash()] = new CachedBlock
            {
                Height = block.Height,
                BlockHash = block.GetHash(),
                PreviousBlockHash = block.Header.PreviousBlockHash
            };
        }

        public void RemoveBlock(Hash blockHash)
        {
            _cachedBlocks.TryRemove(blockHash, out _);
        }

        public void RemoveBlockUnderHeight(long blockHeight)
        {
            var blockHashes = _cachedBlocks.Values.Where(b => b.Height <= blockHeight).Select(b => b.BlockHash).ToList();
            foreach (var blockHash in blockHashes)
            {
                RemoveBlock(blockHash);
            }
        }

        public CachedBlock GetBlock(Hash blockHash)
        {
            _cachedBlocks.TryGetValue(blockHash, out var block);
            //return block?.IsIrreversibleBlock == true ? null : block;
            return block;
        }

        public List<CachedBlock> GetBlocks()
        {
            return _cachedBlocks.Values.ToList();
        }

        public void SetLastIrreversible(Hash blockHash)
        {
            if(!_cachedBlocks.TryGetValue(blockHash,out var block)) return;
            block.IsIrreversibleBlock = true;
        }
    }
    
    public class CachedBlock
    {
        public Hash PreviousBlockHash { get; set; }
        
        public Hash BlockHash { get; set; }
        
        public long Height { get; set; }

        public bool IsIrreversibleBlock { get; set; }
    }
}