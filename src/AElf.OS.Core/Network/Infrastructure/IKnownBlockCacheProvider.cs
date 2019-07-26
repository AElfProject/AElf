using System.Collections.Concurrent;
using System.Linq;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Infrastructure
{
    public interface IKnownBlockCacheProvider
    {
        bool AddKnownBlock(long blockHeight, Hash blockHash, bool hasFork);
        bool TryGetBlockByHeight(long blockHeight, out Hash blockHash);
    }

    public class KnownBlockCacheProvider : IKnownBlockCacheProvider, ISingletonDependency
    {
        private readonly ConcurrentDictionary<long, Hash> _knowBlocks;

        public KnownBlockCacheProvider()
        {
            _knowBlocks = new ConcurrentDictionary<long, Hash>();
        }
        
        public bool AddKnownBlock(long blockHeight, Hash blockHash, bool hasFork)
        {
            // todo find a better algorithm than this
            _knowBlocks[blockHeight] = blockHash;

            while (_knowBlocks.Count > 10)
                _knowBlocks.TryRemove(_knowBlocks.Keys.Min(), out _);

            return true;
        }

        public bool TryGetBlockByHeight(long blockHeight, out Hash blockHash)
        {
            return _knowBlocks.TryGetValue(blockHeight, out blockHash);
        }
    }
}