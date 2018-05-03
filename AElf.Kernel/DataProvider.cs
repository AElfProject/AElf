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
        private readonly string _dataProviderKey;
        
        private readonly IWorldStateManager _worldStateManager;

        private readonly Path _path;
        
        public DataProvider(IAccountDataContext accountDataContext,
            IWorldStateManager worldStateManager,
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
        
        public IDataProvider GetDataProvider(string name)
        {
            return new DataProvider(_accountDataContext, _worldStateManager, name);
        }

        /// <summary>
        /// If blockHash is null, return data of current block height.
        /// </summary>
        /// <param name="keyHash"></param>
        /// <param name="preBlockHash"></param>
        /// <returns></returns>
        public async Task<byte[]> GetAsync(Hash keyHash, Hash preBlockHash)
        {
            var worldState = await _worldStateManager.GetWorldStateAsync(_accountDataContext.ChainId, preBlockHash);
            var pathHash = _path.SetBlockHashToNull().SetDataKey(keyHash).GetPathHash();
            var change = await worldState.GetChangeAsync(pathHash);
            return await _worldStateManager.GetData(change.After);
        }

        public async Task<byte[]> GetAsync(Hash keyHash)
        {
            var pointerHash = await _worldStateManager.GetPointer(_path.SetDataKey(keyHash).GetPathHash());
            return await _worldStateManager.GetData(pointerHash);
        }

        public async Task SetAsync(Hash keyHash, byte[] obj)
        {
            //Clean the path.
            _path.SetBlockHashToNull();
            
            var pathHash = _path.SetBlockHashToNull().SetDataKey(keyHash).GetPathHash();
            var pointerHash = _worldStateManager.GetPointer(_path);
            var hashBefore = await _worldStateManager.GetPointer(pathHash);

            await _worldStateManager.UpdatePointer(pathHash, pointerHash);
            await _worldStateManager.InsertChange(pathHash, hashBefore, pointerHash);
            await _worldStateManager.SetData(pointerHash, obj);
        }
    }
}
