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

        public IHash GetDataProviderHash(string name)
        {
            IHash hash;
            if (_constractMap.TryGetValue(name, out hash))
            {
                return hash;
            }
            return null;
        }

        public byte[] Serialize()
        {
            return SerializeExtensions.Serialize(this);
        }

        public Task SetAsync(IHash key, ISerializable obj)
        {
            throw new NotImplementedException();
        }
    }
}
