using System.Threading.Tasks;

using AElf.Kernel.Managers;

namespace AElf.Kernel
{
    public class DataProvider : IDataProvider
    {
        private readonly IAccountDataContext _accountDataContext;
        private readonly IWorldStateConsole _worldStateConsole;

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

        public DataProvider(IAccountDataContext accountDataContext, IWorldStateConsole worldStateConsole,
            string dataProviderKey = "")
        {
            _worldStateConsole = worldStateConsole;
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
            return new DataProvider(_accountDataContext, _worldStateConsole, dataProviderKey);
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
            var worldState = await _worldStateConsole.GetWorldStateAsync(preBlockHash);
            //Get corresponding path hash
            var pathHash = _path.SetBlockHashToNull().SetDataKey(keyHash).GetPathHash();
            //Using path hash to get Change from WorldState
            var change = await worldState.GetChangeAsync(pathHash);

            return await _worldStateConsole.GetDataAsync(change.After);
        }

        /// <summary>
        /// Get data from current maybe-not-setted-yet "WorldState"
        /// </summary>
        /// <param name="keyHash"></param>
        /// <returns></returns>
        public async Task<byte[]> GetAsync(Hash keyHash)
        {
            var foo = _path.SetDataKey(keyHash).GetPathHash();
            var pointerHash = await _worldStateConsole.GetPointerAsync(foo);
            return await _worldStateConsole.GetDataAsync(pointerHash);
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
            var pointerHashAfter = _worldStateConsole.CalculatePointerHashOfCurrentHeight(_path);

            var preBlockHash = PreBlockHash;
            if (preBlockHash == null)
            {
                PreBlockHash = await _worldStateConsole.GetDataAsync(
                    Path.CalculatePointerForLastBlockHash(_accountDataContext.ChainId));
                preBlockHash = PreBlockHash;
            }

            var change = await _worldStateConsole.GetChangeAsync(pathHash);
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

            await _worldStateConsole.InsertChangeAsync(pathHash, change);
            await _worldStateConsole.SetDataAsync(pointerHashAfter, obj);

            return change;
        }

        public Hash GetPathFor(Hash keyHash)
        {
            var pathHash = _path.SetBlockHashToNull().SetDataKey(keyHash).GetPathHash();

            return pathHash;
        }
    }
}