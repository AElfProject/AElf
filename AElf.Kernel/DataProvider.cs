using AElf.Kernel.Merkle;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public class DataProvider : IDataProvider
    {
        private readonly BinaryMerkleTree<ISerializable> _dataMerkleTree = new BinaryMerkleTree<ISerializable>();

        public Task<ISerializable> GetAsync(IHash key)
        {
            throw new NotImplementedException();
        }

        public Task<IHash<IMerkleTree<ISerializable>>> GetDataMerkleTreeRootAsync()
        {
            return Task.FromResult(_dataMerkleTree.ComputeRootHash());
        }

        public IDataProvider GetDataProvider(string name)
        {
            throw new NotImplementedException();
        }

        public Task SetAsync(IHash key, ISerializable obj)
        {
            throw new NotImplementedException();
        }
    }
}
