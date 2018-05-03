using System;
using System.Collections.Generic;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using AElf.Kernel.Storages;

namespace AElf.Kernel
{
    public class AccountDataProvider : IAccountDataProvider
    {
        private readonly IWorldStateManager _worldStateManager;
        
        public IAccountDataContext Context { get; set; }

        public AccountDataProvider(Hash accountHash, Hash chainId, 
            IAccountContextService accountContextService,
            IWorldStateManager worldStateManager)
        {
            _worldStateManager = worldStateManager;
            Context = accountContextService.GetAccountDataContext(accountHash, chainId, false);
        }

        public IDataProvider GetDataProvider()
        {
            return new DataProvider(Context, _worldStateManager);
        }
    }
}
