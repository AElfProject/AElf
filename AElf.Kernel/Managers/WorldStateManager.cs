using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.Services;
using AElf.Kernel.Storages;

namespace AElf.Kernel.Managers
{
    public class WorldStateManager: IWorldStateManager
    {
        #region Stores and Service
        private readonly IWorldStateStore _worldStateStore;
        private readonly IDataStore _dataStore;
        private readonly IChangesStore _changesStore;

        private readonly IAccountContextService _accountContextService;
        
        /// <summary>
        /// To avoid accessing DataStore so frequently.
        /// </summary>
        private Hash _preBlockHash;

        /// <summary>
        /// A specific key to store previous block hash value.
        /// </summary>
        public Hash HashToGetPreBlockHash => "PreviousBlockHash".CalculateHash();
        #endregion

        public WorldStateManager(IWorldStateStore worldStateStore,
            IAccountContextService accountContextService,
            IChangesStore changesStore, IDataStore dataStore)
        {
            _worldStateStore = worldStateStore;
            _accountContextService = accountContextService;
            _changesStore = changesStore;
            _dataStore = dataStore;

            _preBlockHash = _dataStore.GetDataAsync(HashToGetPreBlockHash)?.Result ?? Hash.Zero;
            
            _dataStore.SetDataAsync(GetHashToGetPathsCount(), ((long)0).ToBytes());
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
            await _changesStore.InsertChangeAsync(pathHash, change);
            
            var countBytes = await _dataStore.GetDataAsync(GetHashToGetPathsCount());
            countBytes = countBytes ??  ((long)0).ToBytes();
            var key = CalculateHashToGetPath(_preBlockHash, countBytes);
            var count = countBytes.ToInt64();
            await _dataStore.SetDataAsync(key, pathHash.Value.ToByteArray());
            count++;
            await _dataStore.SetDataAsync(GetHashToGetPathsCount(), count.ToBytes());
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
        /// <param name="chainId"></param>
        /// <param name="accountHash"></param>
        /// <returns></returns>
        public IAccountDataProvider GetAccountDataProvider(Hash chainId, Hash accountHash)
        {
            return new AccountDataProvider(accountHash, chainId, _accountContextService, this);
        }

        #region Methods about WorldState

        /// <summary>
        /// Get a WorldState instance.
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="blockHash"></param>
        /// <returns></returns>
        public async Task<IWorldState> GetWorldStateAsync(Hash chainId, Hash blockHash)
        {
            return await _worldStateStore.GetWorldStateAsync(chainId, blockHash);
        }
        
        /// <summary>
        /// Capture a ChangesStore instance and generate a ChangesDict,
        /// then set the ChangesDict to WorldStateStore.
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="preBlockHash">At last set preBlockHash to a specific key</param>
        /// <returns></returns>
        public async Task SetWorldStateAsync(Hash chainId, Hash preBlockHash)
        {
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
            await _worldStateStore.InsertWorldStateAsync(chainId, _preBlockHash, dict);
            await _dataStore.SetDataAsync(HashToGetPreBlockHash, preBlockHash.Value.ToArray());

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
            
            for (long i = 0; i < changedPathsCount; i++)
            {
                var key = CalculateHashToGetPath(blockHash, i.ToBytes());
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
        /// <param name="chainId"></param>
        /// <param name="blockHash"></param>
        /// <returns></returns>
        public async Task<List<Change>> GetChangesAsync(Hash chainId, Hash blockHash)
        {
            var paths = await GetPathsAsync(blockHash);
            var worldState = await _worldStateStore.GetWorldStateAsync(chainId, blockHash);
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
       
        #region Provate methods
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
        private async Task<long> GetChangedPathsCountAsync(Hash blockHash)
        {
            var changedPathsCountBytes = await _dataStore.GetDataAsync(GetHashToGetPathsCount(blockHash));
            return changedPathsCountBytes?.ToInt64() ?? 0;
        }

        /// <summary>
        /// Just use the result hash to get the path of a specific block and a specific order of changes.
        /// </summary>
        /// <param name="blockHash"></param>
        /// <param name="countBytes"></param>
        /// <returns></returns>
        private Hash CalculateHashToGetPath(Hash blockHash, byte[] countBytes)
        {
            return blockHash.CombineReverseHashWith(countBytes);
        }
        #endregion
    }
}