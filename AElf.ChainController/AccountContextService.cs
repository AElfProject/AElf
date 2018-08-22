using System.Collections.Concurrent;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Kernel;
using AElf.SmartContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.ChainControllerImpl
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
        public async Task<IAccountDataContext> GetAccountDataContext(Hash account, Hash chainId)
        {
            var key = chainId.CalculateHashWith(account);    
            if (_accountDataContexts.TryGetValue(key, out var ctx))
            {
                return ctx;
            }

            _stateDictator.ChainId = chainId;
            var adp = _stateDictator.GetAccountDataProvider(account);
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
            
            var adp = _stateDictator.GetAccountDataProvider(accountDataContext.Address);

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