﻿using System.Collections.Generic;
 using AElf.Kernel.Extensions;

namespace AElf.Kernel.Services
{
    public class AccountContextService : IAccountContextService
    {
        private readonly Dictionary<Hash, IAccountDataContext> _accountDataContexts =
            new Dictionary<Hash, IAccountDataContext>();
        
        public IAccountDataContext GetAccountDataContext(Hash accountHash, Hash chainId)
        {
            var key = accountHash.CombineHashWith(chainId);
            if (_accountDataContexts.TryGetValue(key, out var ctx))
            {
                return ctx;
            }

            var accountDataContext = new AccountDataContext
            {
                IncreasementId = 0,
                Address = accountHash,
                ChainId = chainId
            };

            _accountDataContexts[key] = accountDataContext;
            return accountDataContext;
        }
    }
}