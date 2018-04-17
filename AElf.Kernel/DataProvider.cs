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
        
        private readonly IPointerCollection _pointerCollection;
        private readonly IWorldStateManager _worldStateManager;
        private readonly IChangesCollection _changesCollection;
        private readonly Hash _preBlockHash;

        private readonly Path _path;
        
        public DataProvider(IAccountDataContext accountDataContext, IPointerCollection pointerCollection, 
            IWorldStateManager worldStateManager, Hash preBlockHash, IChangesCollection changesCollection, 
            string dataProviderKey = "")
        {
            _worldStateManager = worldStateManager;
            _preBlockHash = preBlockHash;
            _changesCollection = changesCollection;
            _pointerCollection = pointerCollection;
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
            return new DataProvider(_accountDataContext, _pointerCollection, _worldStateManager, _preBlockHash, _changesCollection, name);
        }

        /// <summary>
        /// If blockHash is null, return data of current block height.
        /// </summary>
        /// <param name="blockHash"></param>
        /// <returns></returns>
        public async Task<byte[]> GetAsync(Hash blockHash)
        {
            var worldState = await _worldStateManager.GetWorldStateAsync(_accountDataContext.ChainId, blockHash);
            var pathHash = _path.SetBlockHashToNull().GetPathHash();
            var change = await worldState.GetChange(pathHash);
            return await _worldStateManager.GetData(change.After);
        }

        public async Task<byte[]> GetAsync()
        {
            return await _worldStateManager.GetData(await _pointerCollection.GetAsync(_path.GetPathHash()));
        }

        public async Task SetAsync(byte[] obj)
        {
            //Clean the path.
            _path.SetBlockHashToNull();
            var pathHash = _path.GetPathHash();
            var pointerHash = _path.SetBlockHash(_preBlockHash).GetPointerHash();

            var hashBefore = await _pointerCollection.GetAsync(pathHash);

            await _changesCollection.InsertAsync(pathHash, new Change()
            {
                Before = hashBefore,
                After = pointerHash
            });
            
            await _pointerCollection.UpdateAsync(pathHash, pointerHash);

            await _worldStateManager.SetData(pointerHash, obj);
        }
    }
}
