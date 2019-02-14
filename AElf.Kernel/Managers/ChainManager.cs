using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Storages;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Managers
{
    public class ChainManager : IChainManager, ISingletonDependency
    {
        private readonly IChainHeightStore _chainHeightStore;
        private readonly IGenesisBlockHashStore _genesisBlockHashStore;
        private readonly ICurrentBlockHashStore _currentBlockHashStore;
        private readonly ICanonicalStore _canonicalStore;

        public ChainManager(IChainHeightStore chainHeightStore, IGenesisBlockHashStore genesisBlockHashStore,
            ICurrentBlockHashStore currentBlockHashStore, ICanonicalStore canonicalStore)
        {
            _chainHeightStore = chainHeightStore;
            _genesisBlockHashStore = genesisBlockHashStore;
            _currentBlockHashStore = currentBlockHashStore;
            _canonicalStore = canonicalStore;
        }

        public async Task AddChainAsync(int chainId, Hash genesisBlockHash)
        {
            await _genesisBlockHashStore.SetAsync(chainId.ToStorageKey(), genesisBlockHash);
            await UpdateCurrentBlockHashAsync(chainId, genesisBlockHash);
        }

        public async Task UpdateCurrentBlockHashAsync(int chainId, Hash blockHash)
        {
            await _currentBlockHashStore.SetAsync(chainId.ToStorageKey(), blockHash);
        }

        public async Task<Hash> GetCurrentBlockHashAsync(int chainId)
        {
            var hash = await _currentBlockHashStore.GetAsync<Hash>(chainId.ToStorageKey());
            return hash;
        }

        /// <summary>
        /// update block count in this chain not last block index
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public async Task UpdateCurrentBlockHeightAsync(int chainId, ulong height)
        {
            await _chainHeightStore.SetAsync(chainId.ToStorageKey(), new UInt64Value
            {
                Value = height
            });
        }

        /// <summary>
        /// return block count in this chain not last block index
        /// "0" means no block recorded for this chain
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        public async Task<ulong> GetCurrentBlockHeightAsync(int chainId)
        {
            var height = await _chainHeightStore.GetAsync<UInt64Value>(chainId.ToStorageKey());
            return height?.Value ?? 0;
        }

        public async Task SetCanonical(int chainId, ulong height, Hash canonical)
        {
            var key = GetCanonicalKey(chainId, height);
            await _canonicalStore.SetAsync(key, canonical);
        }

        public async Task<Hash> GetCanonical(int chainId, ulong height)
        {
            var key = GetCanonicalKey(chainId, height);
            return await _canonicalStore.GetAsync<Hash>(key);
        }

        public async Task RemoveCanonical(int chainId, ulong height)
        {
            var key = GetCanonicalKey(chainId, height);
            await _canonicalStore.RemoveAsync(key);
        }

        private string GetCanonicalKey(int chainId, ulong height)
        {
            return chainId.ToStorageKey() + height;
        }
    }
}