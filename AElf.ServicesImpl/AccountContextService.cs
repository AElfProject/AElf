using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks; 
using AElf.Kernel.Managers;
using AElf.Kernel.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AElf.Kernel;
using AElf.SmartContract;

namespace AElf.Services
{
    public class AccountContextService : IAccountContextService
    {
        private readonly ConcurrentDictionary<Hash, IAccountDataContext> _accountDataContexts =
            new ConcurrentDictionary<Hash, IAccountDataContext>();

        private readonly IWorldStateDictator _worldStateDictator;

        public AccountContextService(IWorldStateDictator worldStateDictator)
        {   
            _worldStateDictator = worldStateDictator;
        }
        
        /// <inheritdoc/>
        public async Task<IAccountDataContext> GetAccountDataContext(Hash account, Hash chainId)
        {
            var key = chainId.CalculateHashWith(account);    
            if (_accountDataContexts.TryGetValue(key, out var ctx))
            {
                return ctx;
            }
            
            var adp = await _worldStateDictator.GetAccountDataProvider(account);
            var idBytes = await adp.GetDataProvider().GetAsync(GetKeyForIncrementId());
            var id = idBytes == null ? 0 : UInt64Value.Parser.ParseFrom(idBytes).Value;
            
            var accountDataContext = new AccountDataContext
            {
                IncrementId = id,
                Address = account,
                ChainId = chainId
            };

            _accountDataContexts.TryAdd(key, accountDataContext);
            return accountDataContext;
        }

        
        /// <inheritdoc/>
        public async Task SetAccountContext(IAccountDataContext accountDataContext)
        {
            _accountDataContexts.AddOrUpdate(accountDataContext.ChainId.CalculateHashWith(accountDataContext.Address),
                accountDataContext, (hash, context) => accountDataContext);
            
            var adp = await _worldStateDictator.GetAccountDataProvider(accountDataContext.Address);

            //await adp.GetDataProvider().SetAsync(GetKeyForIncrementId(), accountDataContext.IncrementId.ToBytes());
            await adp.GetDataProvider().SetAsync(GetKeyForIncrementId(), new UInt64Value
            {
                Value = accountDataContext.IncrementId
            }.ToByteArray());

        }

        private Hash GetKeyForIncrementId()
        {
            return "Id".CalculateHash();
        }
    }
}