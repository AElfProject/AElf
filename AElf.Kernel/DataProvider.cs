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
        private readonly string _key;
        
        private readonly IPointerStore _pointerStore;
        private readonly IWorldStateStore _worldStateStore;
        private readonly IChangesStore _changesStore;
        private readonly Hash _preBlockHash;

        private readonly Path _path;
        
        public DataProvider(IAccountDataContext accountDataContext, IPointerStore pointerStore, 
            IWorldStateStore worldStateStore, Hash preBlockHash, IChangesStore changesStore, 
            string dataProviderKey = "aelf")
        {
            _worldStateStore = worldStateStore;
            _preBlockHash = preBlockHash;
            _changesStore = changesStore;
            _pointerStore = pointerStore;
            _accountDataContext = accountDataContext;
            _key = dataProviderKey;
            
            _path = new Path()
                .SetChainHash(_accountDataContext.ChainId)
                .SetAccount(_accountDataContext.Address)
                .SetDataProvider(GetHash());

        }

        private Hash GetHash()
        {
            return new Hash(new Hash(_accountDataContext.ChainId.CalculateHashWith(_accountDataContext.Address))
                .CalculateHashWith(_key));
        }
        
        public IDataProvider GetDataProvider(string name)
        {
            return new DataProvider(_accountDataContext, _pointerStore, _worldStateStore, _preBlockHash, _changesStore, name);
        }

        /// <summary>
        /// If blockHash is null, return data of current block height.
        /// </summary>
        /// <param name="blockHash"></param>
        /// <returns></returns>
        public async Task<byte[]> GetAsync(Hash blockHash)
        {
            var worldState = await _worldStateStore.GetWorldState(_accountDataContext.ChainId, blockHash);
            var pathHash = _path.SetBlockHashToNull().GetPathHash();
            var change = await worldState.GetChange(pathHash);
            return await _worldStateStore.GetData(change.After);
        }

        public async Task<byte[]> GetAsync()
        {
            return await _worldStateStore.GetData(await _pointerStore.GetAsync(_path.GetPathHash()));
        }

        public async Task SetAsync(byte[] obj)
        {
            //Clean the path.
            _path.SetBlockHashToNull();
            var pathHash = _path.GetPathHash();
            var pointerHash = _path.SetBlockHash(_preBlockHash).GetPointerHash();

            var hashBefore = await _pointerStore.GetAsync(pathHash);

            await _changesStore.InsertAsync(pathHash, new Change()
            {
                Before = hashBefore,
                After = pointerHash
            });
            
            await _pointerStore.UpdateAsync(pathHash, pointerHash);

            await _worldStateStore.SetData(pointerHash, obj);
        }
    }
}
