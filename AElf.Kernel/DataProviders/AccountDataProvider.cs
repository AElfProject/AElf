using AElf.Kernel.Managers;
using AElf.Kernel.Services;

namespace AElf.Kernel
{
    public class AccountDataProvider : IAccountDataProvider
    {
        private readonly IWorldStateManager _worldStateManager;
        
        public IAccountDataContext Context { get; set; }

        public AccountDataProvider(Hash chainId, Hash accountAddress, 
            IWorldStateManager worldStateManager)
        {
            _worldStateManager = worldStateManager;
            
            //Just use its structure to store info.
            Context = new AccountDataContext
            {
                Address = accountAddress,
                ChainId = chainId
            };

        }

        public IDataProvider GetDataProvider()
        {
            return new DataProvider(Context, _worldStateManager);
        }
    }
}
