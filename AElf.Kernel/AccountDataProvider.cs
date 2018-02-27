using System;

namespace AElf.Kernel
{
    public class AccountDataProvider : IAccountDataProvider
    {
        private readonly IAccount _account;
        private readonly IDataProvider _dataProvider;


        public IAccountDataContext Context { get; set; }

        public AccountDataProvider(IAccount account, WorldState worldState, bool addDataProviderToWorldState = false)
        {
            _account = account;
            Context = new AccountDataContext();
            _dataProvider = new DataProvider(worldState, GetAccountAddress());
            if (addDataProviderToWorldState)
            {
                worldState.AddDataProvider(_dataProvider);
            }
        }
        
        public IHash<IAccount> GetAccountAddress()
        {
            return _account.GetAddress();
        }

        public IDataProvider GetDataProvider()
        {
            return _dataProvider;
        }
    }
}
