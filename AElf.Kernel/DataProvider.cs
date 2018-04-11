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
        
        private readonly Dictionary<Hash, IChangesStore> _changesDictionary;
        private readonly IPointerStore _pointerStore;
        private readonly IWorldStateStore _worldStateStore;

        private readonly Path _path;
        
        public DataProvider(IAccountDataContext accountDataContext, IPointerStore pointerStore, 
            Dictionary<Hash, IChangesStore> changesDictionary, IWorldStateStore worldStateStore, string dataProviderKey = "aelf")
        {
            _changesDictionary = changesDictionary;
            _worldStateStore = worldStateStore;
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
            return new DataProvider(_accountDataContext, _pointerStore, _changesDictionary, _worldStateStore, name);
        }

        /// <summary>
        /// If blockHash is null, return data of current block height.
        /// </summary>
        /// <param name="blockHash"></param>
        /// <returns></returns>
        public async Task<byte[]> GetAsync(Hash blockHash = null)
        {
            var pointerHash = blockHash == null
                ? await _pointerStore.GetAsync(_path.GetPathHash())
                : _path.SetBlockHash(blockHash).GetPointerHash();

            return await _worldStateStore.GetData(pointerHash);
        }

        public async Task SetAsync(Hash currentBlockHash, byte[] obj)
        {
            //Clean the path.
            _path.SetBlockHashToNull();
            var pathHash = _path.GetPathHash();

            _path.SetBlockHash(currentBlockHash);
            var pointerHash = _path.GetPointerHash();

            var hashBefore = _pointerStore.GetAsync(pathHash);
            
            await _pointerStore.InsertAsync(pathHash, pointerHash);

            var change = new Change
            {
                Before = await hashBefore,
                After = pointerHash
            };
            await _changesDictionary[_accountDataContext.ChainId].InsertAsync(pathHash, change);

            await _worldStateStore.SetData(pointerHash, obj);
        }
    }
}
