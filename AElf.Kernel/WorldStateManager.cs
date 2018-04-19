using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using AElf.Kernel.Storages;

namespace AElf.Kernel
{
    public class WorldStateManager: IWorldStateManager
    {
        private readonly IWorldStateStore _worldStateStore;
        private readonly IPointerStore _pointerStore;
        private Hash _preBlockHash;
        private readonly IAccountContextService _accountContextService;
        private IChangesStore _changesStore;
        private readonly IDataStore _dataStore;

        public WorldStateManager(IWorldStateStore worldStateStore, Hash preBlockHash, 
            IAccountContextService accountContextService, IPointerStore pointerStore, IChangesStore changesStore, IDataStore dataStore)
        {
            _worldStateStore = worldStateStore;
            _preBlockHash = preBlockHash;
            _accountContextService = accountContextService;
            _pointerStore = pointerStore;
            _changesStore = changesStore;
            _dataStore = dataStore;
        }
        
        /// <inheritdoc />
        public async Task<WorldState> GetWorldStateAsync(Hash chainId, Hash blockHash)
        {
            return await _worldStateStore.GetWorldState(chainId, blockHash);
        }
        
        /// <inheritdoc />
        public async Task SetWorldStateToCurrentState(Hash chainId, Hash currentBlockHash)
        {
            await _worldStateStore.InsertWorldState(chainId, _preBlockHash, _changesStore);
            _changesStore = new ChangesStore(new KeyValueDatabase(), new KeyValueDatabase());
            _preBlockHash = currentBlockHash;
        }

        /// <inheritdoc />
        public async Task RollbackDataToPreviousWorldState()
        {
            var dict = await _changesStore.GetChangesDictionary();
            foreach (var pair in dict)
            {
                await _pointerStore.UpdateAsync(pair.Key, pair.Value.Before);
            }
        }

        /// <inheritdoc />
        public IAccountDataProvider GetAccountDataProvider(Hash chainId, Hash accountHash)
        {
            return new AccountDataProvider(accountHash, chainId, _accountContextService,
                _pointerStore, this, _preBlockHash, ref _changesStore);
        }
        
        public async Task SetData(Hash pointerHash, byte[] data)
        {
            await _dataStore.SetData(pointerHash, data);
        }

        public async Task<byte[]> GetData(Hash pointerHash)
        {
            return await _dataStore.GetData(pointerHash);
        }

    }
}