using AElf.Kernel.Merkle;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.Storages;

namespace AElf.Kernel
{
    public class DataProvider : IDataProvider
    {
        private readonly IAccountDataContext _accountDataContext;
        private readonly IWorldStateManager _worldStateManager;
        /// <summary>
        /// To dictinct DataProviders of same account and same level.
        /// </summary>
        private readonly string _dataProviderKey;
        private readonly Path _path;
        
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

        private Hash GetHash()
        {
            return _accountDataContext.GetHash().CalculateHashWith(_dataProviderKey);
        }
        
        /// <summary>
        /// Get a sub-level DataProvider.
        /// </summary>
        /// <param name="name">sub-level DataProvider's name</param>
        /// <returns></returns>
        public IDataProvider GetDataProvider(string name)
        {
            return new DataProvider(_accountDataContext, _worldStateManager, name);
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
            var worldState = await _worldStateManager.GetWorldStateAsync(_accountDataContext.ChainId, preBlockHash);
            //Get corresponding path hash
            var pathHash = _path.SetBlockHashToNull().SetDataKey(keyHash).GetPathHash();
            //Using path hash to get Change from WorldState
            var change = await worldState.GetChangeAsync(pathHash);
            
            return await _worldStateManager.GetData(change.After);
        }

        /// <summary>
        /// Get data from current maybe-not-setted-yet "WorldState"
        /// </summary>
        /// <param name="keyHash"></param>
        /// <returns></returns>
        public async Task<byte[]> GetAsync(Hash keyHash)
        {
            var pointerHash = await _worldStateManager.GetPointerFromPointerStore(_path.SetDataKey(keyHash).GetPathHash());
            return await _worldStateManager.GetData(pointerHash);
        }

        /// <summary>
        /// Set a data and return a related Change.
        /// </summary>
        /// <param name="keyHash"></param>
        /// <param name="obj"></param>
        /// <param name="transactionHash"></param>
        /// <returns></returns>
        public async Task<Change> SetAsync(Hash keyHash, byte[] obj)
        {
            //Clean the path.
            _path.SetBlockHashToNull();
            
            var pathHash = _path.SetBlockHashToNull().SetDataKey(keyHash).GetPathHash();
            var pointerHashBefore = await _worldStateManager.GetPointerFromPointerStore(pathHash);
            var pointerHashAfter = _worldStateManager.CalculatePointerHashOfCurrentHeight(_path);

            var change = new Change
            {
                Before = pointerHashBefore,
                After = pointerHashAfter,
            };

            await _worldStateManager.UpdatePointerToPointerStore(pathHash, pointerHashAfter);
            await _worldStateManager.InsertChange(pathHash, change);
            await _worldStateManager.SetData(pointerHashAfter, obj);
            
            return change;
        }
    }
}
