using AElf.Kernel.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public class DataProvider : IDataProvider
    {
        private List<ISerializable> _data = new List<ISerializable>();
        private BinaryMerkleTree<ISerializable> _dataMerkleTree = new BinaryMerkleTree<ISerializable>();

        private Dictionary<string, IHash> _constractMap = new Dictionary<string, IHash>();

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
            IHash hash;
            if (_constractMap.TryGetValue(name, out hash))
            {
                return WorldState.GetDataProvider(_constractMap[name]);
            }
            else
            {
                hash = GenerateDataProviderHash();
                _constractMap[name] = hash;
                return WorldState.GetDataProvider(hash);
            }
        }

        public byte[] Serialize()
        {
            return SerializationExtensions.Serialize(this);
        }

        public Task SetAsync(IHash key, ISerializable obj)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Somehow to generate the data provider's hash value.
        /// </summary>
        /// <returns></returns>
        private IHash GenerateDataProviderHash()
        {
            throw new NotImplementedException();
        }
    }
}
