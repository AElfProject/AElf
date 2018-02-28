using AElf.Kernel.Merkle;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;

namespace AElf.Kernel
{
    public class DataProvider : IDataProvider
    {
        private readonly IHash<IAccount> _accountAddress;
        private readonly BinaryMerkleTree<ISerializable> _dataMerkleTree = new BinaryMerkleTree<ISerializable>();
        private readonly Dictionary<string, IDataProvider> _dataProviders = new Dictionary<string, IDataProvider>();
        private readonly Dictionary<IHash, IHash> _mapSerializedValue = new Dictionary<IHash, IHash>();

        private IHash _keyHash;
        private IHash _newValueHash;

        private WorldState _worldState;

        /// <summary>
        /// ctor.
        /// </summary>
        /// <param name="accountAddress"></param>
        /// <param name="worldState"></param>
        public DataProvider(WorldState worldState, IHash<IAccount> accountAddress)
        {
            _keyHash = null;
            _newValueHash = null;

            _worldState = worldState;
            _accountAddress = accountAddress;
        }

        /// <summary>
        /// Directly fetch a data from k-v database.
        /// We will use the key to calculate a hash to act as the address.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Task<ISerializable> GetAsync(string key)
        {
            return GetAsync(new Hash<string>(_accountAddress.CalculateHashWith(key)));
        }

        /// <summary>
        /// Directly fetch a data from k-v database.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Task<ISerializable> GetAsync(IHash key)
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

        public Task<IHash<IMerkleTree<ISerializable>>> GetDataMerkleTreeRootAsync()
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
        /// <param name="dataProvider"></param>
        public void SetDataProvider(string name, IDataProvider dataProvider)
        {
            _dataProviders[name] = dataProvider;
            _worldState.AddDataProvider(dataProvider);
        }

        /// <summary>
        /// Directly add a data to k-v database.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public Task SetAsync(IHash key, ISerializable obj)
        {
            var beforeSet = this;
            //Add the hash of value to merkle tree.
            var newMerkleNode = new Hash<ISerializable>(obj.CalculateHash());
            var oldMerkleNode = new Hash<ISerializable>(GetAsync(key).CalculateHash());
            _dataMerkleTree.UpdateNode(oldMerkleNode, newMerkleNode);

            //Re-calculate the hash with the value, 
            //and use _mapSerializedValue to map the key with the value's truely address in database.
            //Thus we can use the same key to get it's value (after updated).
            var finalHash = new Hash<ISerializable>(key.CalculateHashWith(obj));

            #region Store the context
            _keyHash = key;
            _newValueHash = finalHash;
            #endregion
            
            Execute();
            
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
        public Task SetAsync(string key, ISerializable obj)
        {
            return SetAsync(new Hash<string>(_accountAddress.CalculateHashWith(key)), obj);
        }
        
        /// <summary>
        /// Call this method after sucessfully execute the related transaction.
        /// But in this way we can only set one k-v pair in one transaction.
        /// </summary>
        private void Execute()
        {
            if (_keyHash == null || _newValueHash == null)
            {
                return;
            }
            _mapSerializedValue[_keyHash] = _newValueHash;
        }
    }
}
