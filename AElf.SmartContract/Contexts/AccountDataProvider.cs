using AElf.Kernel;

namespace AElf.SmartContract
{
    public class AccountDataProvider : IAccountDataProvider
    {
        private readonly IStateDictator _stateDictator;
        
        public IAccountDataContext Context { get; set; }

        public AccountDataProvider(Hash chainId, Hash accountAddress, 
            IStateDictator stateDictator)
        {
            _stateDictator = stateDictator;

            //Just use its structure to store info.
            Context = new AccountDataContext
            {
                Address = accountAddress,
                ChainId = chainId
            };

        }

        public IDataProvider GetDataProvider()
        {
            return new DataProvider(Context, _stateDictator);
        }
    }
}
