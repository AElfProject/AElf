using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.Managers;

namespace AElf.Kernel
{
    public class DataProvider : IDataProvider
    {
        private readonly IAccountDataContext _accountDataContext;
        private readonly IWorldStateManager _worldStateManager;
        /// <summary>
        /// To dictinct DataProviders of same account and same level.
        /// Using a string value is just a choise, actually we can use any type of value.
        /// </summary>
        private readonly string _dataProviderKey;
        private readonly Path _path;

        /// <summary>
        /// Will only set the value during setting data.
        /// </summary>
        private Hash PreBlockHash { get; set; }

        public DataProvider(IAccountDataContext accountDataContext, IWorldStateManager worldStateManager, 
            string dataProviderKey = "")
        {
            _worldStateManager = worldStateManager;
            _accountDataContext = accountDataContext;
            _dataProviderKey = dataProviderKey;

            _path = new Path()
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
            return new DataProvider(_accountDataContext, _worldStateManager, dataProviderKey);
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
            var worldState = await _worldStateManager.GetWorldStateAsync(preBlockHash);
            //Get corresponding path hash
            var pathHash = _path.SetBlockHashToNull().SetDataKey(keyHash).GetPathHash();
            //Using path hash to get Change from WorldState
            var change = await worldState.GetChangeAsync(pathHash);
            
            return await _worldStateManager.GetDataAsync(change.After);
        }

        /// <summary>
        /// Get data from current maybe-not-setted-yet "WorldState"
        /// </summary>
        /// <param name="keyHash"></param>
        /// <returns></returns>
        public async Task<byte[]> GetAsync(Hash keyHash)
        {
            var foo = _path.SetDataKey(keyHash).GetPathHash();
            var pointerHash = await _worldStateManager.GetPointerAsync(foo);
            return await _worldStateManager.GetDataAsync(pointerHash);
        }

        /// <summary>
        /// Set a data and return a related Change.
        /// </summary>
        /// <param name="keyHash"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public async Task<Change> SetAsync(Hash keyHash, byte[] obj)
        {
            //Clean the path.
            _path.SetBlockHashToNull();
            
            //Generate the path hash.
            var pathHash = _path.SetBlockHashToNull().SetDataKey(keyHash).GetPathHash();

            //Generate the new pointer hash (using previous block hash)
            var pointerHashAfter = _worldStateManager.CalculatePointerHashOfCurrentHeight(_path);

            var preBlockHash = PreBlockHash;
            if (preBlockHash == null)
            {
                PreBlockHash = await _worldStateManager.GetDataAsync(
                    Path.CalculatePointerForLastBlockHash(_accountDataContext.ChainId));
                preBlockHash = PreBlockHash;
            }
            
            var change = await _worldStateManager.GetChangeAsync(pathHash);
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
                if (preBlockHash != change.LatestChangedBlockHash)
                {
                    change.ClearChangeBefores();
                }
                
                change.UpdateHashAfter(pointerHashAfter);
            }
            
            change.LatestChangedBlockHash = preBlockHash;
            
            await _worldStateManager.InsertChangeAsync(pathHash, change);
            await _worldStateManager.SetDataAsync(pointerHashAfter, obj);
            
            return change;
        }
    }
}
