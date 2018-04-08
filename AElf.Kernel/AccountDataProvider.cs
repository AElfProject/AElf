using System;

namespace AElf.Kernel
{
    public class AccountDataProvider : IAccountDataProvider
    {
        public IAccountDataContext Context { get; set; }
        
        public IHash GetAccountAddress()
        {
            throw new NotImplementedException();
        }

        public IDataProvider GetDataProvider()
        {
            throw new NotImplementedException();
        }
    }
}
