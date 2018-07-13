using AElf.Kernel.Managers;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public class AccountDataProvider : IAccountDataProvider
    {
        private readonly IWorldStateDictator _worldStateDictator;
        
        public IAccountDataContext Context { get; set; }

        public AccountDataProvider(Hash chainId, Hash accountAddress, 
            IWorldStateDictator worldStateDictator)
        {
            _worldStateDictator = worldStateDictator;

            //Just use its structure to store info.
            Context = new AccountDataContext
            {
                Address = accountAddress,
                ChainId = chainId
            };

        }

        public IDataProvider GetDataProvider()
        {
            return new DataProvider(Context, _worldStateDictator);
        }
    }
}
