using System;

namespace AElf.Kernel
{
    public class AccountDataProvider : IAccountDataProvider
    {
        private readonly IAccount _account;
        private readonly IDataProvider _dataProvider;
        
        public IAccountDataContext Context { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public AccountDataProvider(IAccount account)
        {
            _account = account;
            _dataProvider = new DataProvider(account);
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
