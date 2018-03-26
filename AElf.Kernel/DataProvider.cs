using AElf.Kernel.Merkle;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;

namespace AElf.Kernel
{
    public class DataProvider : IDataProvider
    {
        private readonly IHash _accountAddress;
        private readonly BinaryMerkleTree _dataMerkleTree = new BinaryMerkleTree();
        private readonly Dictionary<string, IDataProvider> _dataProviders = new Dictionary<string, IDataProvider>();
        private readonly Dictionary<IHash, IHash> _mapSerializedValue = new Dictionary<IHash, IHash>();

        private readonly IWorldState _worldState;

        /// <summary>
        /// ctor.
        /// </summary>
        /// <param name="accountAddress"></param>
        /// <param name="worldState"></param>
        public DataProvider(IWorldState worldState, IHash accountAddress)
        {
            _worldState = worldState;
            _accountAddress = accountAddress;
        }

        /// <summary>
        /// Directly fetch a data from k-v database.
        /// We will use the key to calculate a hash to act as the address.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Task<byte[]> GetAsync(string key)
        {
            return GetAsync(new Hash(_accountAddress.CalculateHashWith(key)));
        }

        /// <summary>
        /// Directly fetch a data from k-v database.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Task<byte[]> GetAsync(IHash key)
        {
            foreach (var k in _mapSerializedValue.Keys)
            {
                if (k.Equals(key))
                {
                    return Task.FromResult(Database.Select(_mapSerializedValue[k]));
                }
            }
            return Task.FromResult(Database.Select(null));
        }

        public Task<Hash> GetDataMerkleTreeRootAsync()
        {
            return Task.FromResult(_dataMerkleTree.ComputeRootHash());
        }

        /// <summary>
        /// If the data provider of given name is exists, then return the data provider,
        /// otherwise create a new one and return.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IDataProvider GetDataProvider(string name)
        {
            return _dataProviders.TryGetValue(name, out var dataProvider) ? dataProvider : AddDataProvider(name);
        }

        /// <summary>
        /// Create a new data provider and add it to dict.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private IDataProvider AddDataProvider(string name)
        {
            var beforeAdd = this;
            
            var defaultDataProvider = new DataProvider(_worldState, _accountAddress);
            _dataProviders[name] = defaultDataProvider;
            
            _worldState.AddDataProvider(defaultDataProvider);
            _worldState.UpdateDataProvider(beforeAdd, this);
            
            return defaultDataProvider;
        }
        
        /// <summary>
        /// Set a data provider.
        /// </summary>
        /// <param name="name"></param>
        public void SetDataProvider(string name)
        {
            var dataProvider = new DataProvider(_worldState, _accountAddress);
            _dataProviders[name] = dataProvider;
            _worldState.AddDataProvider(dataProvider);
        }

        /// <summary>
        /// Directly add a data to k-v database.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public Task SetAsync(IHash key, byte[] obj)
        {
            var beforeSet = this;
            
            //Add the hash of value to merkle tree.
            var newMerkleNode = new Hash(obj.CalculateHash());
            var oldMerkleNode = new Hash(GetAsync(key).CalculateHash());
            _dataMerkleTree.UpdateNode(oldMerkleNode, newMerkleNode);

            //Re-calculate the hash with the value, 
            //and use _mapSerializedValue to map the key with the value's truely address in database.
            //Thus we can use the same key to get it's value (after updated).
            var finalHash = new Hash(key.CalculateHashWith(obj));
            
            _mapSerializedValue[key] = finalHash;
            
            _worldState.UpdateDataProvider(beforeSet, this);

            Database.Insert(finalHash, obj);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Directly add a data to k-v database.
        /// We will use the key to calculate a hash to act as the address.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public Task SetAsync(string key, byte[] obj)
        {
            return SetAsync(new Hash(_accountAddress.CalculateHashWith(key)), obj);
        }
    }
}
