using AElf.Kernel.Managers;
using AElf.Kernel.Services;

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
            Context = accountContextService.GetAccountDataContext(accountHash, chainId);
        }

        public IDataProvider GetDataProvider()
        {
            return new DataProvider(Context, _worldStateManager);
        }
    }
}
