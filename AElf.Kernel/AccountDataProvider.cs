using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using AElf.Kernel.Storages;

namespace AElf.Kernel
{
    public class AccountDataProvider : IAccountDataProvider
    {
        private readonly IPointerStore _pointerStore;
        private readonly IWorldStateManager _worldStateManager;
        private readonly Hash _preBlockHash;
        private IChangesStore _changesStore;
        
        public IAccountDataContext Context { get; set; }

        public AccountDataProvider(Hash accountHash, Hash chainId, 
            IAccountContextService accountContextService, IPointerStore pointerStore,
            IWorldStateManager worldStateManager, Hash preBlockHash, ref IChangesStore changesStore)
        {
            _worldStateManager = worldStateManager;
            _preBlockHash = preBlockHash;
            _changesStore = changesStore;
            _pointerStore = pointerStore;
            Context = accountContextService.GetAccountDataContext(accountHash, chainId, false);
        }

        public IDataProvider GetDataProvider()
        {
            return new DataProvider(Context, _pointerStore, _worldStateManager, _preBlockHash, ref _changesStore);
        }
    }
}
