using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Manager.Interfaces;
using AElf.Kernel.Storage.Interfaces;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Manager.Managers
{
    public class ChainManager : IChainManager
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

        public async Task AddChainAsync(Hash chainId, Hash genesisBlockHash)
        {
            await _genesisBlockHashStore.SetAsync(chainId.DumpHex(), genesisBlockHash);
            await UpdateCurrentBlockHashAsync(chainId, genesisBlockHash);
        }

        public async Task UpdateCurrentBlockHashAsync(Hash chainId, Hash blockHash)
        {
            await _currentBlockHashStore.SetAsync(chainId.DumpHex(), blockHash);
        }

        public async Task<Hash> GetCurrentBlockHashAsync(Hash chainId)
        {
            var hash = await _currentBlockHashStore.GetAsync<Hash>(chainId.DumpHex());
            return hash;
        }

        /// <summary>
        /// update block count in this chain not last block index
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public async Task UpdateCurrentBlockHeightAsync(Hash chainId, ulong height)
        {
            await _chainHeightStore.SetAsync(chainId.DumpHex(), new UInt64Value
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
        public async Task<ulong> GetCurrentBlockHeightAsync(Hash chainId)
        {
            var height = await _chainHeightStore.GetAsync<UInt64Value>(chainId.DumpHex());
            return height?.Value ?? 0;
        }

        public async Task SetCanonical(Hash chainId, ulong height, Hash canonical)
        {
            var key = GetCanonicalKey(chainId, height);
            await _canonicalStore.SetAsync(key, canonical);
        }

        public async Task<Hash> GetCanonical(Hash chainId, ulong height)
        {
            var key = GetCanonicalKey(chainId, height);
            return await _canonicalStore.GetAsync<Hash>(key);
        }

        public async Task RemoveCanonical(Hash chainId, ulong height)
        {
            var key = GetCanonicalKey(chainId, height);
            await _canonicalStore.RemoveAsync(key);
        }

        private string GetCanonicalKey(Hash chainId, ulong height)
        {
            return chainId.DumpHex() + height;
        }
    }
}