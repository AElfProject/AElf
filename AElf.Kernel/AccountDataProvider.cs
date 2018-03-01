using System;

namespace AElf.Kernel
{
    public class AccountDataProvider : IAccountDataProvider
    {
        private readonly IAccount _account;
        private IDataProvider _dataProvider;

        public IAccountDataContext Context { get; set; }

        public AccountDataProvider(IAccount account, WorldState worldState)
        {
            _account = account;
            Context = new AccountDataContext();
            _dataProvider = new DataProvider(account, worldState);
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
