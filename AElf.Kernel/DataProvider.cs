using AElf.Kernel.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public class DataProvider : IDataProvider
    {
        private Dictionary<IHash, ISerializable> _data = new Dictionary<IHash, ISerializable>();
        private BinaryMerkleTree<ISerializable> _dataMerkleTree = new BinaryMerkleTree<ISerializable>();

        private Dictionary<string, IHash> _constractMap = new Dictionary<string, IHash>();

        public Task<ISerializable> GetAsync(IHash key)
        {
            ISerializable result;
            if (_data.TryGetValue(key, out result))
            {
                return Task.FromResult(result);
            }
            else
            {
                return null;
            }
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

        public Task SetAsync(IHash key, ISerializable obj)
        {
            return Task.FromResult(_data[key] = obj);
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
