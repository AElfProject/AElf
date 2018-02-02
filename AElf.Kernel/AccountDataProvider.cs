using AElf.Kernel.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public class AccountDataProvider : IAccountDataProvider
    {
        private Dictionary<string, IHash> _constractMap = new Dictionary<string, IHash>();

        public IAccountDataContext Context { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IHash<IAccount> GetAccountAddress()
        {
            throw new NotImplementedException();
        }

        public IDataProvider GetDataProvider()
        {
            throw new NotImplementedException();
        }

        public byte[] Serialize()
        {
            return SerializationExtensions.Serialize(this);
        }
    }
}
