﻿using System;
 using System.Collections.Generic;
 using System.Threading.Tasks;
 using AElf.Kernel.Extensions;
 using AElf.Kernel.Managers;

namespace AElf.Kernel.Services
{
    public class AccountContextService : IAccountContextService
    {
        private readonly Dictionary<Hash, IAccountDataContext> _accountDataContexts =
            new Dictionary<Hash, IAccountDataContext>();

        private readonly IWorldStateManager _worldStateManager;

        public AccountContextService(IWorldStateManager worldStateManager)
        {   
            _worldStateManager = worldStateManager;
        }
        
        /// <inheritdoc/>
        public async Task<IAccountDataContext> GetAccountDataContext(Hash account, Hash chainId)
        {   
                
            var key = account.CombineHashWith(chainId);
            if (_accountDataContexts.TryGetValue(key, out var ctx))
             {
                return ctx;
            }
            
            await _worldStateManager.OfChain(chainId);
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
        public async Task SetAccountContext(IAccountDataContext accountDataContext)
        {
            // calculate key
            var key = accountDataContext.Address.CombineHashWith(accountDataContext.ChainId);

            _accountDataContexts[key] = accountDataContext;
            
            await _worldStateManager.OfChain(accountDataContext.ChainId);
            var adp = _worldStateManager.GetAccountDataProvider(accountDataContext.Address);

            await adp.GetDataProvider().SetAsync(GetKeyForIncrementId(), accountDataContext.IncrementId.ToBytes());
        }

        private Hash GetKeyForIncrementId()
        {
            return "Id".CalculateHash();
        }
    }
}