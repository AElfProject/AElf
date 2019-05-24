using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public interface IRandomHashCacheService
    {
        void SetRandomHash(Hash bestChainBlockHash, Hash randomHash);
        Hash GetRandomHash(Hash bestChainBlockHash);
        void SetGeneratedBlockPreviousBlockInformation(Hash blockHash, long blockHeight);
        Hash GetLatestGeneratedBlockRandomHash();
    }

    public class RandomHashCacheService : IRandomHashCacheService
    {
        private readonly ConcurrentDictionary<Hash, Hash> _randomHashes = new ConcurrentDictionary<Hash, Hash>();
        private readonly ConcurrentDictionary<long, Hash> _blockHashes = new ConcurrentDictionary<long, Hash>();

        public ILogger<RandomHashCacheService> Logger { get; set; }

        public void SetRandomHash(Hash bestChainBlockHash, Hash randomHash)
        {
            // Only keep one before setting.
            _randomHashes.RemoveAll(p => p.Key != _randomHashes.Keys.Last());
            _randomHashes.TryAdd(bestChainBlockHash, randomHash);

            Logger.LogTrace(
                $"Setting. Block hash {bestChainBlockHash} - Random hash {randomHash}. Count of cached random hashes: {_randomHashes.Count}");
        }

        public Hash GetRandomHash(Hash bestChainBlockHash)
        {
            _randomHashes.TryGetValue(bestChainBlockHash, out var randomHash);
            Logger.LogTrace($"Getting. Block hash {bestChainBlockHash} - Random hash {randomHash}");
            return randomHash ?? Hash.Empty;
        }

        public void SetGeneratedBlockPreviousBlockInformation(Hash blockHash, long blockHeight)
        {
            if (_blockHashes.Count > 0)
            {
                _blockHashes.RemoveAll(p => p.Key < _blockHashes.OrderByDescending(h => h.Key).First().Key);
            }
            Logger.LogTrace(
                $"Count of cached block hashes: {_blockHashes.Count}");
            _blockHashes.TryAdd(blockHeight, blockHash);
        }

        public Hash GetLatestGeneratedBlockRandomHash()
        {
            if ( _blockHashes.Count == 0)
            {
                return Hash.Empty;
            }

            var blockHash = _blockHashes.OrderByDescending(p => p.Key).First().Value;
            return GetRandomHash(blockHash);
        }
    }
}