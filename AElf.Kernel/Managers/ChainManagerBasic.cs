using System;
using System.Threading.Tasks;
using AElf.Kernel.Storages;
using Google.Protobuf.WellKnownTypes;
using NLog;

namespace AElf.Kernel.Managers
{
    public class ChainManagerBasic : IChainManagerBasic
    {
        private readonly IDataStore _dataStore;
        private readonly ILogger _logger;
        private readonly Hash _sideChainIdListKey = "SideChainIdList".CalculateHash();

        public ChainManagerBasic(IDataStore dataStore, ILogger logger = null)
        {
            _dataStore = dataStore;
            _logger = logger;
        }

        public async Task AddChainAsync(Hash chainId, Hash genesisBlockHash)
        {
            await _dataStore.InsertAsync(chainId.OfType(HashType.GenesisHash), genesisBlockHash);
            await UpdateCurrentBlockHashAsync(chainId, genesisBlockHash);
        }

        public async Task<Hash> GetGenesisBlockHashAsync(Hash chainId)
        {
            return await _dataStore.GetAsync<Hash>(chainId.OfType(HashType.GenesisHash));
        }

        public async Task UpdateCurrentBlockHashAsync(Hash chainId, Hash blockHash)
        {
            var key = chainId.OfType(HashType.CurrentHash);
            await _dataStore.InsertAsync(key, blockHash);
        }
        
        public async Task<Hash> GetCurrentBlockHashAsync(Hash chainId)
        {
            var key = chainId.OfType(HashType.CurrentHash);
            var hash = await _dataStore.GetAsync<Hash>(key);
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
            var key = chainId.OfType(HashType.ChainHeight);
            await _dataStore.InsertAsync(key, new UInt64Value
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
            var key = chainId.OfType(HashType.ChainHeight);
            var height = await _dataStore.GetAsync<UInt64Value>(key);
            return height?.Value ?? 0;
        }
        
        public async Task AddSideChainId(Hash chainId)
        {
            var idList = await GetSideChainIdList();
            idList = idList ?? new SideChainIdList();
            idList.ChainIds.Add(chainId);
            await _dataStore.InsertAsync(_sideChainIdListKey, idList);
        }

        public async Task<SideChainIdList> GetSideChainIdList()
        {
            return await _dataStore.GetAsync<SideChainIdList>(_sideChainIdListKey);
        }
    }
}