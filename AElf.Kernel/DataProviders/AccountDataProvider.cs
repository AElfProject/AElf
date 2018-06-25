using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using AElf.Kernel.Types;

namespace AElf.Kernel
{
    public class AccountDataProvider : IAccountDataProvider
    {
        private readonly IWorldStateConsole _worldStateConsole;
        
        public IAccountDataContext Context { get; set; }

        public AccountDataProvider(Hash chainId, Hash accountAddress, 
            IWorldStateConsole worldStateConsole)
        {
            _worldStateConsole = worldStateConsole;
            
            //Just use its structure to store info.
            Context = new AccountDataContext
            {
                Address = accountAddress,
                ChainId = chainId
            };

        }

        public IDataProvider GetDataProvider()
        {
            return new DataProvider(Context, _worldStateConsole);
        }
    }
}
