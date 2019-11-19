using System.Collections.Generic;
using System.Linq;
using System.Text;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public interface IRandomHashCacheService
    {
        void SetRandomHash(Hash bestChainHash, Hash randomHash);
        Hash GetRandomHash(Hash bestChainBlockHash);
        void SetGeneratedBlockBestChainHash(Hash blockHash, long blockHeight);
        Hash GetLatestGeneratedBlockRandomHash();
    }

    public class RandomHashCacheService : IRandomHashCacheService, ISingletonDependency
    {
        private readonly Dictionary<Hash, Hash> _randomHashes = new Dictionary<Hash, Hash>();
        private readonly Dictionary<long, Hash> _blockHashes = new Dictionary<long, Hash>();

        public ILogger<RandomHashCacheService> Logger { get; set; }

        public void SetRandomHash(Hash bestChainHash, Hash randomHash)
        {
            _randomHashes.RemoveAll(p => !_blockHashes.Values.Contains(p.Key));
            _randomHashes[bestChainHash] = randomHash;

            // Following are just logs.

            {
                var log = new StringBuilder("\n");
                foreach (var hashLink in _blockHashes)
                {
                    log.Append($"{hashLink.Key} - {hashLink.Value.ToHex()}\n");
                }

                Logger.LogTrace($"Block hash links:{log}");
            }

            {
                var log = new StringBuilder("\n");
                foreach (var hashLink in _randomHashes)
                {
                    log.Append($"{hashLink.Key} - {hashLink.Value.ToHex()}\n");
                }

                Logger.LogTrace($"Random hash links:{log}");
            }
        }

        public Hash GetRandomHash(Hash bestChainBlockHash)
        {
            _randomHashes.TryGetValue(bestChainBlockHash, out var randomHash);
            return randomHash ?? Hash.Empty;
        }

        public void SetGeneratedBlockBestChainHash(Hash blockHash, long blockHeight)
        {
            if (_blockHashes.Count > 0)
            {
                var highestHeight = _blockHashes.OrderByDescending(h => h.Key).First().Key;
                _blockHashes.RemoveAll(p => p.Key < highestHeight);
            }

            _blockHashes[blockHeight] = blockHash;
        }

        public Hash GetLatestGeneratedBlockRandomHash()
        {
            if (_blockHashes.Count == 1)
            {
                return Hash.Empty;
            }

            var blockHash = _blockHashes.OrderBy(p => p.Key).First().Value;
            var randomHash = GetRandomHash(blockHash);

            return randomHash;
        }
    }
}