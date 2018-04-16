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
        private readonly IWorldStateStore _worldStateStore;
        private readonly Hash _preBlockHash;
        private readonly IChangesStore _changesStore;
        
        public IAccountDataContext Context { get; set; }

        public AccountDataProvider(Hash accountHash, Hash chainId, 
            IAccountContextService accountContextService, IPointerStore pointerStore,
            IWorldStateStore worldStateStore, Hash preBlockHash, IChangesStore changesStore)
        {
            _worldStateStore = worldStateStore;
            _preBlockHash = preBlockHash;
            _changesStore = changesStore;
            _pointerStore = pointerStore;
            Context = accountContextService.GetAccountDataContext(accountHash, chainId, false);
        }

        public IDataProvider GetDataProvider()
        {
            return new DataProvider(Context, _pointerStore, _worldStateStore, _preBlockHash, _changesStore);
        }
    }
}
