using System;
using AElf.Kernel.Consensus.Application;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public class AEDPoSBlockTimeProvider : IBlockTimeProvider, ISingletonDependency
    {
        private readonly MemoryCache _blockTimeCache;
        private readonly ConsensusOptions _consensusOptions;

        public AEDPoSBlockTimeProvider(IOptionsSnapshot<ConsensusOptions> consensusOptions)
        {
            _consensusOptions = consensusOptions.Value;
            _blockTimeCache = new MemoryCache(new MemoryCacheOptions
            {
                ExpirationScanFrequency = TimeSpan.FromMilliseconds(_consensusOptions.MiningInterval)
            });
        }

        public Timestamp GetBlockTime(Hash blockHash)
        {
            if (blockHash != null && _blockTimeCache.TryGetValue(blockHash, out var blockTime))
            {
                return blockTime as Timestamp;
            }

            return new Timestamp();
        }

        public void SetBlockTime(Timestamp blockTime, Hash blockHash)
        {
            _blockTimeCache.Set(blockHash, blockTime, TimeSpan.FromMilliseconds(_consensusOptions.MiningInterval));
        }
    }
}