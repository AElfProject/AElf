using AElf.Kernel.Merkle;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;

namespace AElf.Kernel
{
    public class DataProvider : IDataProvider
    {
        private readonly IAccount _account;
        private BinaryMerkleTree<ISerializable> _dataMerkleTree = new BinaryMerkleTree<ISerializable>();
        private Dictionary<string, IDataProvider> _dataProviders = new Dictionary<string, IDataProvider>();
        private Dictionary<IHash, IHash> _mapSerializedValue = new Dictionary<IHash, IHash>();

        private IHash _keyHash;
        private IHash _newValueHash;

        /// <summary>
        /// ctor.
        /// </summary>
        /// <param name="account"></param>
        public DataProvider(IAccount account)
        {
            _account = account;

            _keyHash = null;
            _newValueHash = null;
        }

        /// <summary>
        /// Directly fetch a data from k-v database.
        /// We will use the key to calculate a hash to act as the address.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Task<ISerializable> GetAsync(string key)
        {
            return GetAsync(new Hash<string>(_account.GetAddress().CalculateHashWith(key)));
        }

        /// <summary>
        /// Directly fetch a data from k-v database.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Task<ISerializable> GetAsync(IHash key)
        {
            return _mapSerializedValue.TryGetValue(key, out var finalHash) ? 
                Task.FromResult(Database.Select(finalHash)) :
                Task.FromResult(Database.Select(null));
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
            
            var defaultDataProvider = new DataProvider(_account);
            _dataProviders[name] = defaultDataProvider;
            
            WorldState.Instance.AddDataProvider(defaultDataProvider);
            WorldState.Instance.UpdateDataProvider(beforeAdd, this);
            
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
            
            WorldState.Instance.UpdateDataProvider(beforeSet, this);

            return new Task(() => Database.Insert(finalHash, obj));
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
            return SetAsync(new Hash<string>(_account.GetAddress().CalculateHashWith(key)), obj);
        }
        
        /// <summary>
        /// Call this method after sucessfully execute the related transaction.
        /// But in this way we can only set one k-v pair in one transaction.
        /// </summary>
        public void Execute()
        {
            if (_keyHash == default(IHash) || _newValueHash == default(IHash))
            {
                return;
            }
            _mapSerializedValue[_keyHash] = _newValueHash;
        }
    }
}
