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
        private readonly IChangesStore _changesStore;

        public WorldStateManager(IWorldStateStore worldStateStore, Hash preBlockHash, 
            IAccountContextService accountContextService, IPointerStore pointerStore, IChangesStore changesStore)
        {
            _worldStateStore = worldStateStore;
            _preBlockHash = preBlockHash;
            _accountContextService = accountContextService;
            _pointerStore = pointerStore;
            _changesStore = changesStore;
        }

        public async Task<WorldState> GetWorldStateAsync(Hash chainId)
        {
            return await _worldStateStore.GetWorldState(chainId, _preBlockHash);
        }
        
        public async Task<WorldState> GetWorldStateAsync(Hash chainId, Hash blockHash)
        {
            return await _worldStateStore.GetWorldState(chainId, blockHash);
        }

        public IAccountDataProvider GetAccountDataProvider(Hash chainId, Hash accountHash)
        {
            return new AccountDataProvider(accountHash, chainId, _accountContextService,
                _pointerStore, _worldStateStore, _preBlockHash, _changesStore);
        }

        public async Task SetWorldStateToCurrentState(Hash chainId, Hash newBlockHash)
        {
            await _worldStateStore.InsertWorldState(chainId, _preBlockHash, _changesStore);
            _preBlockHash = newBlockHash;
        }
    }
}