using System;
using System.Threading.Tasks;
using AElf.Kernel;

// ReSharper disable once CheckNamespace
namespace AElf.SmartContract
{
    public class DataProvider : IDataProvider
    {
        private readonly IAccountDataContext _accountDataContext;
        private readonly IWorldStateDictator _worldStateDictator;

        /// <summary>
        /// To dictinct DataProviders of same account and same level.
        /// Using a string value is just a choise, actually we can use any type of value, even integer.
        /// </summary>
        private readonly string _dataProviderKey;

        private readonly IResourcePath _resourcePath;

        public DataProvider(IAccountDataContext accountDataContext, IWorldStateDictator worldStateDictator,
            string dataProviderKey = "")
        {
            _worldStateDictator = worldStateDictator;
            _accountDataContext = accountDataContext;
            _dataProviderKey = dataProviderKey;

            _resourcePath = new ResourcePath()
                .SetChainId(_accountDataContext.ChainId)
                .SetBlockProducerAddress(worldStateDictator.BlockProducerAccountAddress)
                .SetAccountAddress(_accountDataContext.Address)
                .SetDataProvider(GetHash());
        }

        public Hash GetHash()
        {
            // Use AccountDataContext instance + _dataProviderKey to calculate DataProvider's hash.
            return _accountDataContext.GetHash().CalculateHashWith(_dataProviderKey);
        }

        /// <summary>
        /// Get a sub-level DataProvider.
        /// </summary>
        /// <param name="dataProviderKey">sub-level DataProvider's name</param>
        /// <returns></returns>
        public IDataProvider GetDataProvider(string dataProviderKey)
        {
            return new DataProvider(_accountDataContext, _worldStateDictator, dataProviderKey);
        }

        /// <summary>
        /// Get data of specifix block by corresponding block hash.
        /// </summary>
        /// <param name="keyHash"></param>
        /// <param name="preBlockHash"></param>
        /// <returns></returns>
        public async Task<byte[]> GetAsync(Hash keyHash, Hash preBlockHash)
        {
            //Get correspoding WorldState instance
            var worldState = await _worldStateDictator.GetWorldStateAsync(preBlockHash);
            //Get corresponding path hash
            var pathHash = _resourcePath.RevertPointerToPath().SetDataKey(keyHash).GetPathHash();
            //Using path hash to get Change from WorldState
            var pointerHash = worldState.GetPointerHash(pathHash);

            return await _worldStateDictator.GetDataAsync(pointerHash);
        }

        /// <summary>
        /// Get data from current maybe-not-setted-yet "WorldState"
        /// </summary>
        /// <param name="keyHash"></param>
        /// <returns></returns>
        public async Task<byte[]> GetAsync(Hash keyHash)
        {
            var pointerHash = await _worldStateDictator.GetPointerAsync(GetPathFor(keyHash));
            return await _worldStateDictator.GetDataAsync(pointerHash);
        }

        /// <summary>
        /// Set a data and return a related Change.
        /// </summary>
        /// <param name="keyHash"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public async Task SetAsync(Hash keyHash, byte[] obj)
        {
            throw new NotImplementedException();
        }

        public Hash GetPathFor(Hash keyHash)
        {
            var pathHash = _resourcePath.RevertPointerToPath().SetDataKey(keyHash).GetPathHash();

            return pathHash;
        }
    }
}