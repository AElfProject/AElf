using System;

namespace AElf.Kernel
{
    public class AccountDataProvider : IAccountDataProvider
    {
        public IAccountDataContext Context { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IHash<IAccount> GetAccountAddress()
        {
            throw new NotImplementedException();
        }

        public IDataProvider GetDataProvider()
        {
            throw new NotImplementedException();
        }
    }
}
