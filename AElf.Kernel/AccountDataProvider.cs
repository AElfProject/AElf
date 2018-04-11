using System;
using System.Collections.Generic;
using System.Net.Mime;
using AElf.Kernel.Storages;

namespace AElf.Kernel
{
    public class AccountDataProvider : IAccountDataProvider
    {
        private readonly IPointerStore _pointerStore;
        private readonly IWorldStateStore _worldStateStore;
        
        public IAccountDataContext Context { get; set; }

        public AccountDataProvider(Hash accountHash, Hash chainId, 
            IAccountContextService accountContextService, IPointerStore pointerStore,
            IWorldStateStore worldStateStore)
        {
            _worldStateStore = worldStateStore;
            _pointerStore = pointerStore;
            Context = accountContextService.GetAccountDataContext(accountHash, chainId, false);
        }
        
        public IHash GetAccountAddress()
        {
            return Context.Address;
        }

        public IDataProvider GetDataProvider()
        {
            return new DataProvider(Context, _pointerStore, _worldStateStore);
        }
    }
}
