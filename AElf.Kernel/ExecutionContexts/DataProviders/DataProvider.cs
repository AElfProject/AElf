using System.Threading.Tasks;

using AElf.Kernel.Managers;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public class DataProvider : IDataProvider
    {
        private readonly IAccountDataContext _accountDataContext;
        private readonly IWorldStateDictator _worldStateDictator;

        /// <summary>
        /// To dictinct DataProviders of same account and same level.
        /// Using a string value is just a choise, actually we can use any type of value.
        /// </summary>
        private readonly string _dataProviderKey;

        private readonly PathContextService _pathContextService;

        /// <summary>
        /// Will only set the value during setting data.
        /// </summary>
        private Hash PreBlockHash { get; set; }

        public DataProvider(IAccountDataContext accountDataContext, IWorldStateDictator worldStateDictator,
            string dataProviderKey = "")
        {
            _worldStateDictator = worldStateDictator;
            _accountDataContext = accountDataContext;
            _dataProviderKey = dataProviderKey;

            _pathContextService = new PathContextService()
                .SetChainHash(_accountDataContext.ChainId)
                .SetAccount(_accountDataContext.Address)
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
            var pathHash = _pathContextService.RevertPointerToPath().SetDataKey(keyHash).GetPathHash();
            //Using path hash to get Change from WorldState
            var change = await worldState.GetChangeAsync(pathHash);

            return await _worldStateDictator.GetDataAsync(change.After);
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
        public async Task<Change> SetAsync(Hash keyHash, byte[] obj)
        {
            //Generate the path hash.
            var pathHash = GetPathFor(keyHash);

            //Generate the new pointer hash (using previous block hash)
            var pointerHashAfter =await _worldStateDictator.CalculatePointerHashOfCurrentHeight(_pathContextService);

            var preBlockHash = PreBlockHash;
            if (preBlockHash == null)
            {
                PreBlockHash = await _worldStateDictator.GetDataAsync(
                    PathContextService.CalculatePointerForLastBlockHash(_accountDataContext.ChainId));
                preBlockHash = PreBlockHash;
            }
            

            var change = await _worldStateDictator.GetChangeAsync(pathHash);
            if (change == null)
            {
                change = new Change
                {
                    After = pointerHashAfter
                };
            }
            else
            {
                //See whether the latest changes of this Change happened in this height,
                //If not, clear the change, because this Change is too old to support rollback.
                if (_worldStateDictator.DeleteChangeBeforesImmidiately || preBlockHash != change.LatestChangedBlockHash)
                {
                    change.ClearChangeBefores();
                }
                
                change.UpdateHashAfter(pointerHashAfter);
            }

            change.LatestChangedBlockHash = preBlockHash;

            await _worldStateDictator.InsertChangeAsync(pathHash, change);
            await _worldStateDictator.SetDataAsync(pointerHashAfter, obj);

            return change;
        }

        public Hash GetPathFor(Hash keyHash)
        {
            var pathHash = _pathContextService.RevertPointerToPath().SetDataKey(keyHash).GetPathHash();

            return pathHash;
        }
    }
}