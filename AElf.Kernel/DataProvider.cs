using AElf.Kernel.Merkle;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;

namespace AElf.Kernel
{
    public class DataProvider : IDataProvider
    {
        public IDataProvider GetDataProvider(string name)
        {
            throw new NotImplementedException();
        }

        public void SetDataProvider(string name)
        {
            throw new NotImplementedException();
        }

        public Task<ISerializable> GetAsync(IHash key)
        {
            throw new NotImplementedException();
        }

        public Task SetAsync(IHash key, ISerializable obj)
        {
            throw new NotImplementedException();
        }

        public Task<IHash<IMerkleTree<ISerializable>>> GetDataMerkleTreeRootAsync()
        {
            throw new NotImplementedException();
        }
    }
}
