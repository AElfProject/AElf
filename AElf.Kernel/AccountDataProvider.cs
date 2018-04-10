using System;
using System.Collections.Generic;
using System.Net.Mime;
using AElf.Kernel.Storages;

namespace AElf.Kernel
{
    public class AccountDataProvider : IAccountDataProvider
    {
        private readonly Dictionary<Hash, IChangesStore> _changesDictionary;
        private readonly IPointerStore _pointerStore;
        
        public IAccountDataContext Context { get; set; }

        public AccountDataProvider(Hash accountHash, Hash chainId, 
            IAccountContextService accountContextService, IPointerStore pointerStore,
            Dictionary<Hash, IChangesStore> changesDictionary)
        {
            _changesDictionary = changesDictionary;
            _pointerStore = pointerStore;
            Context = accountContextService.GetAccountDataContext(accountHash, chainId, false);
        }
        
        public IHash GetAccountAddress()
        {
            return Context.Address;
        }

        public IDataProvider GetDataProvider()
        {
            return new DataProvider(Context, _pointerStore, _changesDictionary);
        }
    }
}
