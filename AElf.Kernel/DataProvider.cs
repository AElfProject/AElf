using AElf.Kernel.Merkle;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;

namespace AElf.Kernel
{
    public class DataProvider : IDataProvider
    {
        private readonly byte[] _accountAddress;
        private BinaryMerkleTree<ISerializable> _dataMerkleTree = new BinaryMerkleTree<ISerializable>();
        private Dictionary<string, IDataProvider> _dataProviders = new Dictionary<string, IDataProvider>();
        
        /// <summary>
        /// ctor.
        /// </summary>
        /// <param name="accountAddress"></param>
        public DataProvider(byte[] accountAddress)
        {
            _accountAddress = accountAddress;
        }

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
            return Task.FromResult(Database.Select(key));
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
            var defaultDataProvider = new DataProvider(_accountAddress);
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
            _dataMerkleTree.AddNode(new Hash<ISerializable>(obj.CalculateHash()));
            return new Task(() => Database.Insert(key, obj));
        }

        public Task SetAsync(string key, ISerializable obj)
        {
            return SetAsync(new Hash<string>(_accountAddress.CalculateHashWith(key)), obj);
        }
    }
}
