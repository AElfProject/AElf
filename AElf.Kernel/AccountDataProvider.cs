using System;

namespace AElf.Kernel
{
    public class AccountDataProvider : IAccountDataProvider
    {
        private readonly byte[] _accountAddress;
        private readonly IDataProvider _dataProvider;
        
        public IAccountDataContext Context { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public AccountDataProvider(byte[] accountAddress)
        {
            _accountAddress = accountAddress;
            _dataProvider = new DataProvider(accountAddress);
        }
        
        public byte[] GetAccountAddress()
        {
            return _accountAddress;
        }

        public IDataProvider GetDataProvider()
        {
            return _dataProvider;
        }
    }
}
