using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common;
using AElf.SmartContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.ChainController
{
    public class AccountContextService : IAccountContextService
    {
        private readonly ConcurrentDictionary<Hash, IAccountDataContext> _accountDataContexts =
            new ConcurrentDictionary<Hash, IAccountDataContext>();

        private readonly IStateDictator _stateDictator;

        public AccountContextService(IStateDictator stateDictator)
        {   
            _stateDictator = stateDictator;
        }
        
        /// <inheritdoc/>
        public async Task<IAccountDataContext> GetAccountDataContext(Address accountAddress, Hash chainId)
        {
            var key = Hash.FromHashes(chainId, Hash.FromMessage(accountAddress));    
            if (_accountDataContexts.TryGetValue(key, out var ctx))
            {
                return ctx;
            }

            _stateDictator.ChainId = chainId;
            var adp = _stateDictator.GetAccountDataProvider(accountAddress);
            var idBytes = await adp.GetDataProvider().GetAsync<UInt64Value>(GetKeyForIncrementId());
            var id = idBytes == null ? 0 : UInt64Value.Parser.ParseFrom(idBytes).Value;
            
            var accountDataContext = new AccountDataContext
            {
                IncrementId = id,
                Address = accountAddress,
                ChainId = chainId
            };

            _accountDataContexts.TryAdd(key, accountDataContext);
            return accountDataContext;
        }

        
        /// <inheritdoc/>
        public async Task SetAccountContext(IAccountDataContext accountDataContext)
        {
            _accountDataContexts.AddOrUpdate(
                Hash.FromHashes(accountDataContext.ChainId, Hash.FromMessage(accountDataContext.Address)),
                accountDataContext, (hash, context) => accountDataContext);

            var adp = _stateDictator.GetAccountDataProvider(accountDataContext.Address);

            //await adp.GetDataProvider().SetAsync(GetKeyForIncrementId(), accountDataContext.IncrementId.ToBytes());
            await adp.GetDataProvider().SetDataAsync(GetKeyForIncrementId(), new UInt64Value
            {
                Value = accountDataContext.IncrementId
            });

        }

        private Hash GetKeyForIncrementId()
        {
            return Hash.FromString("Id");
        }
    }
}