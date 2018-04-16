using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public class AccountContextService : IAccountContextService
    {
        private readonly Dictionary<Hash, IAccountDataContext> _accountDataContexts =
            new Dictionary<Hash, IAccountDataContext>();
        
        public IAccountDataContext GetAccountDataContext(Hash accountHash, Hash chainId, bool plusIncreasmentId = false)
        {
            if (_accountDataContexts.TryGetValue(accountHash, out var ctx))
            {
                if (!plusIncreasmentId) 
                    return ctx;
                
                var newCtx = ctx;
                newCtx.IncreasementId++;
                _accountDataContexts[accountHash] = newCtx;
                
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