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

        /// <summary>
        /// ctor.
        /// </summary>
        /// <param name="accountAddress"></param>
        public DataProvider(IAccount account)
        {
            _account = account;
        }

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
            var defaultDataProvider = new DataProvider(_account);
            _dataProviders[name] = defaultDataProvider;
            return defaultDataProvider;
        }

        /// <summary>
        /// Directly add a data to k-v database.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public Task SetAsync(IHash key, ISerializable obj)
        {
            //Add the hash of value to merkle tree.
            _dataMerkleTree.AddNode(new Hash<ISerializable>(obj.CalculateHash()));
            //Re-calculate the hash with the value, 
            //and use _mapSerializedValue to map the key with the value's truely address in database.
            //Thus we can use the same key to get it's value (after updated).
            var finalHash = new Hash<ISerializable>(key.CalculateHashWith(obj));
            _mapSerializedValue[key] = finalHash;
            return new Task(() => Database.Insert(finalHash, obj));
        }

        public Task SetAsync(string key, ISerializable obj)
        {
            return SetAsync(new Hash<string>(_account.GetAddress().CalculateHashWith(key)), obj);
        }
    }
}
