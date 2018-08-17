using System;
using System.Threading.Tasks;
using AElf.Kernel;

// ReSharper disable once CheckNamespace
namespace AElf.SmartContract
{
    public class DataProvider : IDataProvider
    {
        private readonly IResourcePath _resourcePath;

        private readonly IStateDictator _stateDictator;

        /// <summary>
        /// To dictinct DataProviders of same account and same level.
        /// Using a string value is just a choise, actually we can use any type of value, even integer.
        /// </summary>
        private readonly string _dataProviderKey;

        public DataProvider(IResourcePath resourcePath, IStateDictator stateDictator, string dataProviderKey = "")
        {
            _stateDictator = stateDictator;
            _dataProviderKey = dataProviderKey;
            _resourcePath = resourcePath;
        }

        private Hash GetHash()
        {
            return _accountDataContext.GetHash().CalculateHashWith(_dataProviderKey);
        }

        /// <summary>
        /// Get a sub-level DataProvider.
        /// </summary>
        /// <param name="dataProviderKey">sub-level DataProvider's name</param>
        /// <returns></returns>
        public IDataProvider GetDataProvider(string dataProviderKey)
        {
            return new DataProvider(_resourcePath, _stateDictator, dataProviderKey);
        }

        /// <summary>
        /// Get data from current maybe-not-setted-yet "WorldState"
        /// </summary>
        /// <param name="keyHash"></param>
        /// <returns></returns>
        public async Task<byte[]> GetAsync(Hash keyHash)
        {
            var pointerHash = await _stateDictator.GetPointerAsync(GetPathFor(keyHash));
            return await _stateDictator.GetDataAsync(pointerHash);
        }

        /// <summary>
        /// Set a data
        /// </summary>
        /// <param name="keyHash"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public async Task SetAsync(Hash keyHash, byte[] obj)
        {
            
        }
    }
}