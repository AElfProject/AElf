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
        private readonly Hash _preBlockHash;
        private readonly IAccountContextService _accountContextService;

        public WorldStateManager(IWorldStateStore worldStateStore, Hash preBlockHash, 
            IAccountContextService accountContextService, IPointerStore pointerStore)
        {
            _worldStateStore = worldStateStore;
            _preBlockHash = preBlockHash;
            _accountContextService = accountContextService;
            _pointerStore = pointerStore;
        }

        public Task<WorldState> GetWorldStateAsync(Hash chainId)
        {
            return Task.FromResult(_worldStateStore.GetWorldState(chainId, _preBlockHash));
        }

        public IAccountDataProvider GetAccountDataProvider(Hash chainId, Hash accountHash)
        {
            return new AccountDataProvider(accountHash, chainId, _accountContextService,
                _pointerStore, _worldStateStore, _preBlockHash);
        }
    }
}