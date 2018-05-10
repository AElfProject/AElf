﻿using System.Collections.Generic;

namespace AElf.Kernel.Services
{
    public class AccountContextService : IAccountContextService
    {
        private readonly Dictionary<Hash, IAccountDataContext> _accountDataContexts =
            new Dictionary<Hash, IAccountDataContext>();
        
        public IAccountDataContext GetAccountDataContext(Hash accountHash, Hash chainId)
        {
            
            if (_accountDataContexts.TryGetValue(accountHash, out var ctx))
            {
                return ctx;
            }

            var accountDataContext = new AccountDataContext
            {
                IncreasementId = 0,
                Address = accountHash,
                ChainId = chainId
            };

            _accountDataContexts[accountHash] = accountDataContext;
            return accountDataContext;
        }
    }
}