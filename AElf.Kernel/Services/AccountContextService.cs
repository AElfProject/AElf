﻿using System;
 using System.Collections.Concurrent;
 using System.Collections.Generic;
 using System.Threading.Tasks;
 using AElf.Kernel.Extensions;
 using AElf.Kernel.Managers;

namespace AElf.Kernel.Services
{
    public class AccountContextService : IAccountContextService
    {
        private readonly ConcurrentDictionary<Hash, IAccountDataContext> _accountDataContexts =
            new ConcurrentDictionary<Hash, IAccountDataContext>();

        private readonly IWorldStateManager _worldStateManager;

        public AccountContextService(IWorldStateManager worldStateManager)
        {   
            _worldStateManager = worldStateManager;
        }
        
        /// <inheritdoc/>
        public async Task<IAccountDataContext> GetAccountDataContextAsync(Hash account, Hash chainId)
        {
            var key = chainId.CalculateHashWith(account);    
            if (_accountDataContexts.TryGetValue(key, out var ctx))
             {
                return ctx;
            }
            
            await _worldStateManager.OfChainAsync(chainId);
            var adp = _worldStateManager.GetAccountDataProvider(account);

            var idBytes = await adp.GetDataProvider().GetAsync(GetKeyForIncrementId());
            var id = idBytes?.ToUInt64() ?? 0;
            
            var accountDataContext = new AccountDataContext
            {
                IncrementId = id,
                Address = account,
                ChainId = chainId
            };

            _accountDataContexts[key] = accountDataContext;
            return accountDataContext;
        }

        
        /// <inheritdoc/>
        public async Task SetAccountContextAsync(IAccountDataContext accountDataContext)
        {
            _accountDataContexts.AddOrUpdate(accountDataContext.ChainId.CalculateHashWith(accountDataContext.Address),
                accountDataContext, (hash, context) => accountDataContext);
            
            await _worldStateManager.OfChainAsync(accountDataContext.ChainId);
            var adp = _worldStateManager.GetAccountDataProvider(accountDataContext.Address);

            await adp.GetDataProvider().SetAsync(GetKeyForIncrementId(), accountDataContext.IncrementId.ToBytes());
        }

        private Hash GetKeyForIncrementId()
        {
            return "Id".CalculateHash();
        }
    }
}