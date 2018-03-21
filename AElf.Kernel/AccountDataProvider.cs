using System;

namespace AElf.Kernel
{
    public class AccountDataProvider : IAccountDataProvider
    {
        private readonly IAccount _account;
        private readonly IDataProvider _dataProvider;

        public IAccountDataContext Context { get; set; }


        public AccountDataProvider(IAccount account, IWorldState worldState)
        {
            _account = account;
            Context = new AccountDataContext();
            _dataProvider = new DataProvider(worldState, GetAccountAddress());
        }
        
        public IHash GetAccountAddress()
        {
            return _account.GetAddress();
        }

        public IDataProvider GetDataProvider()
        {
            return _dataProvider;
        }
    }
}
