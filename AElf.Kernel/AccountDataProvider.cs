using AElf.Kernel.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

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
