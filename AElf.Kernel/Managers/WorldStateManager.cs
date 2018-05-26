using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.Storages;

namespace AElf.Kernel.Managers
{
    public class WorldStateManager: IWorldStateManager
    {
        #region Stores
        private readonly IWorldStateStore _worldStateStore;
        private readonly IDataStore _dataStore;
        private readonly IChangesStore _changesStore;
        #endregion
        
        private bool _isChainIdSetted;
        private Hash _chainId;
        private Hash _preBlockHash;

        public WorldStateManager(IWorldStateStore worldStateStore,
            IChangesStore changesStore, IDataStore dataStore)
        {
            _worldStateStore = worldStateStore;
            _changesStore = changesStore;
            _dataStore = dataStore;
        }

        public async Task<IWorldStateManager> OfChain(Hash chainId)
        {
            _chainId = chainId;
            _preBlockHash = await _dataStore.GetDataAsync(await _dataStore.GetDataAsync(
                Path.CalculatePointerForLastBlockHash(chainId)));

            await _dataStore.SetDataAsync(Path.CalculatePointerForPathsCount(_chainId, _preBlockHash), ((ulong)0).ToBytes());

            _isChainIdSetted = true;

            return this;
        }
        
        /// <summary>
        /// Insert a Change to ChangesStore.
        /// And refresh the paths count of current world state,
        /// as well as insert a changed path to DataStore.
        /// The key to get the changed path can be calculated by _preBlockHash and the order.
        /// </summary>
        /// <param name="pathHash"></param>
        /// <param name="change"></param>
        /// <returns></returns>
        public async Task InsertChangeAsync(Hash pathHash, Change change)
        {
            Check();
            
            await _changesStore.InsertChangeAsync(pathHash, change);
            
            var countBytes = await _dataStore.GetDataAsync(Path.CalculatePointerForPathsCount(_chainId, _preBlockHash));
            countBytes = countBytes ??  ((ulong)0).ToBytes();
            var key = CalculateKeyForPath(_preBlockHash, countBytes);
            var count = countBytes.ToUInt64();
            await _dataStore.SetDataAsync(key, pathHash.Value.ToByteArray());
            count++;
            await _dataStore.SetDataAsync(Path.CalculatePointerForPathsCount(_chainId, _preBlockHash), count.ToBytes());
        }

        public async Task<Change> GetChangeAsync(Hash pathHash)
        {
            return await _changesStore.GetChangeAsync(pathHash);
        }
        
        /// <summary>
        /// Rollback changes of executed transactions
        /// by rollback the PointerStore.
        /// </summary>
        /// <returns></returns>
        public async Task RollbackCurrentChangesAsync()
        {
            var dict = await GetChangesDictionaryAsync();
            foreach (var pair in dict)
            {
                await _changesStore.UpdatePointerAsync(pair.Key, pair.Value.Befores[0]);
            }
        }

        /// <summary>
        /// Get an AccountDataProvider instance
        /// </summary>
        /// <param name="accountAddress"></param>
        /// <returns></returns>
        public IAccountDataProvider GetAccountDataProvider(Hash accountAddress)
        {
            Check();
            
            return new AccountDataProvider(_chainId, accountAddress, this);
        }

        #region Methods about WorldState

        /// <summary>
        /// Get a WorldState instance.
        /// </summary>
        /// <param name="blockHash"></param>
        /// <returns></returns>
        public async Task<IWorldState> GetWorldStateAsync(Hash blockHash)
        {
            Check();
            
            return await _worldStateStore.GetWorldStateAsync(_chainId, blockHash);
        }
        
        /// <summary>
        /// Capture a ChangesStore instance and generate a ChangesDict,
        /// then set the ChangesDict to WorldStateStore.
        /// </summary>
        /// <param name="preBlockHash">At last set preBlockHash to a specific key</param>
        /// <returns></returns>
        public async Task SetWorldStateAsync(Hash preBlockHash)
        {
            Check();
            
            var changes = await GetChangesDictionaryAsync();
            var dict = new ChangesDict();
            foreach (var pair in changes)
            {
                var pairHashChange = new PairHashChange
                {
                    Key = pair.Key.Clone(),
                    Value = pair.Value.Clone()
                };
                dict.Dict.Add(pairHashChange);
            }
            await _worldStateStore.InsertWorldStateAsync(_chainId, _preBlockHash, dict);
            
            //Refresh _preBlockHash after setting WorldState.
            _preBlockHash = preBlockHash;
        }
        #endregion

        #region Methods about PointerStore
        /// <summary>
        /// Update the PointerStore
        /// </summary>
        /// <param name="pathHash"></param>
        /// <param name="pointerHash"></param>
        /// <returns></returns>
        public async Task UpdatePointerAsync(Hash pathHash, Hash pointerHash)
        {
            await _changesStore.UpdatePointerAsync(pathHash, pointerHash);
        }

        /// <summary>
        /// Using path hash value to get a pointer hash value from PointerStore.
        /// The pointer hash value represents a actual address of database.
        /// </summary>
        /// <param name="pathHash"></param>
        /// <returns></returns>
        public async Task<Hash> GetPointerAsync(Hash pathHash)
        {
            return await _changesStore.GetPointerAsync(pathHash);
        }
        #endregion

        #region Methods about DataStore
        /// <summary>
        /// Using a pointer hash value like a key to set a byte array to DataStore.
        /// </summary>
        /// <param name="pointerHash"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task SetDataAsync(Hash pointerHash, byte[] data)
        {
            await _dataStore.SetDataAsync(pointerHash, data);
        }

        /// <summary>
        /// Using a pointer hash value to get data from DataStore.
        /// </summary>
        /// <param name="pointerHash"></param>
        /// <returns></returns>
        public async Task<byte[]> GetDataAsync(Hash pointerHash)
        {
            return await _dataStore.GetDataAsync(pointerHash);
        }
        
        /// <summary>
        /// blockHash + order = key.
        /// Using key to get path from DataSotre.
        /// Then return all the paths.
        /// </summary>
        /// <param name="blockHash"></param>
        /// <returns></returns>
        public async Task<List<Hash>> GetPathsAsync(Hash blockHash = null)
        {
            Interlocked.CompareExchange(ref blockHash, _preBlockHash, null);
            
            var paths = new List<Hash>();

            var changedPathsCount = await GetChangedPathsCountAsync(blockHash);
            
            for (ulong i = 0; i < changedPathsCount; i++)
            {
                var key = CalculateKeyForPath(blockHash, i.ToBytes());
                var path = await _dataStore.GetDataAsync(key);
                paths.Add(path);
            }

            return paths;
        }
        #endregion

        #region Get Changes
        /// <summary>
        /// Using a paths list to get Changes from a ChangesStore.
        /// </summary>
        /// <param name="blockHash"></param>
        /// <returns></returns>
        public async Task<List<Change>> GetChangesAsync(Hash blockHash)
        {
            Check();

            var paths = await GetPathsAsync(blockHash);
            var worldState = await _worldStateStore.GetWorldStateAsync(_chainId, blockHash);
            var changes = new List<Change>();
            foreach (var path in paths)
            {
                var change = await worldState.GetChangeAsync(path);
                changes.Add(change);
            }

            return changes;
        }

        /// <summary>
        /// Get Changes from current _changesStore.
        /// </summary>
        /// <returns></returns>
        public async Task<List<Change>> GetChangesAsync()
        {
            var paths = await GetPathsAsync();
            var changes = new List<Change>();
            if (paths == null)
                return changes;
            
            foreach (var path in paths)
            {
                var change = await _changesStore.GetChangeAsync(path);
                changes.Add(change);
            }

            return changes;
        }

        /// <summary>
        /// Get Dictionary of path - Change of current _changesStore.
        /// </summary>
        /// <returns></returns>
        public async Task<Dictionary<Hash, Change>> GetChangesDictionaryAsync()
        {
            var paths = await GetPathsAsync();
            var dict = new Dictionary<Hash, Change>();
            if (paths == null)
            {
                return dict;
            }
            
            foreach (var path in paths)
            {
                var change = await _changesStore.GetChangeAsync(path);
                dict[path] = change;
            }

            return dict;
        }
        #endregion

        /// <summary>
        /// The normal way to get a pointer hash value from a Path instance.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Hash CalculatePointerHashOfCurrentHeight(Path path)
        {
            return path.SetBlockHash(_preBlockHash).GetPointerHash();
        }
       
        #region Private methods
        
        /// <summary>
        /// A specific way to get a hash value which pointer to
        /// the count of Changes of a world state.
        /// </summary>
        /// <param name="blockHash"></param>
        /// <returns></returns>
        private Hash GetHashToGetPathsCount(Hash blockHash = null)
        {
            Interlocked.CompareExchange(ref blockHash, _preBlockHash, null);
            Hash foo = "paths".CalculateHash();
            return foo.CombineHashWith(blockHash);
        }

        /// <summary>
        /// Get the count of changed-paths of a specific block.
        /// </summary>
        /// <param name="blockHash"></param>
        /// <returns></returns>
        private async Task<ulong> GetChangedPathsCountAsync(Hash blockHash)
        {
            Check();
            
            var changedPathsCountBytes = await _dataStore.GetDataAsync(Path.CalculatePointerForPathsCount(_chainId, blockHash));
            return changedPathsCountBytes?.ToUInt64() ?? 0;
        }

        /// <summary>
        /// Just use the result hash to get the path of a specific block and a specific order of changes.
        /// </summary>
        /// <param name="blockHash"></param>
        /// <param name="countBytes"></param>
        /// <returns></returns>
        private Hash CalculateKeyForPath(Hash blockHash, byte[] countBytes)
        {
            return blockHash.CombineReverseHashWith(countBytes);
        }

        private void Check()
        {
            if (!_isChainIdSetted)
            {
                throw new InvalidOperationException("Should set chain id before using a WorldStateManager");
            }
        }
        #endregion
    }
}