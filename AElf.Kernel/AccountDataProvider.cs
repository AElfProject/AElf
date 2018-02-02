using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public class AccountDataProvider : IAccountDataProvider
    {
        public IHash<IAccount> GetAccountAddress()
        {
            throw new NotImplementedException();
        }

        public Task<ISerializable> GetAsync(IHash key)
        {
            throw new NotImplementedException();
        }

        public Task<IHash<IMerkleTree<ISerializable>>> GetDataMerkleTreeRootAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IDataProvider> GetMapAsync(string name)
        {
            throw new NotImplementedException();
        }

        public Task SetAsync(IHash key, ISerializable obj)
        {
            throw new NotImplementedException();
        }
    }
}
