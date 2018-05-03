using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.Storages;

namespace AElf.Kernel
{
    public class WorldStateManager: IWorldStateManager
    {
        private readonly IWorldStateStore _worldStateStore;
        private readonly IPointerStore _pointerStore;
        private readonly IAccountContextService _accountContextService;
        private readonly IDataStore _dataStore;
        
        private Hash _preBlockHash;
        private IChangesStore _changesStore;

        /// <summary>
        /// A specific key to store previous block hash value.
        /// </summary>
        private static readonly Hash HashToGetPreBlockHash = "PreviousBlockHash".CalculateHash();

        public WorldStateManager(IWorldStateStore worldStateStore,
            IAccountContextService accountContextService, IPointerStore pointerStore, 
            IChangesStore changesStore, IDataStore dataStore)
        {
            _worldStateStore = worldStateStore;
            _accountContextService = accountContextService;
            _pointerStore = pointerStore;
            _changesStore = changesStore;
            _dataStore = dataStore;

            _preBlockHash = _dataStore.GetData(HashToGetPreBlockHash).Result ?? Hash.Zero;

            _dataStore.SetData(GetHashToGetPathsCount(), LongToBytes(0));
        }
        
        /// <summary>
        /// Get a WorldState instance.
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="blockHash"></param>
        /// <returns></returns>
        public async Task<IWorldState> GetWorldStateAsync(Hash chainId, Hash blockHash)
        {
            var changesStore =  await _worldStateStore.GetWorldState(chainId, blockHash);
            var paths = await GetPathsAsync(blockHash);
            return new WorldState(changesStore, paths);
        }
        
        /// <summary>
        /// Capture a ChangesStore instance and set it to WorldStateStore,
        /// then renew _changesStore to collect next block's changes.
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="preBlockHash">At last set preBlockHash to a specific key</param>
        /// <returns></returns>
        public async Task SetWorldStateToCurrentState(Hash chainId, Hash preBlockHash)
        {
            await _worldStateStore.InsertWorldState(chainId, _preBlockHash, _changesStore);
            _changesStore = new ChangesStore(new KeyValueDatabase());
            _preBlockHash = preBlockHash;
            await _dataStore.SetData(HashToGetPreBlockHash, preBlockHash.Value.ToByteArray());
        }

        /// <summary>
        /// Update the PointerStore
        /// </summary>
        /// <param name="pathHash"></param>
        /// <param name="pointerHash"></param>
        /// <returns></returns>
        public async Task UpdatePointer(Hash pathHash, Hash pointerHash)
        {
            await _pointerStore.UpdateAsync(pathHash, pointerHash);
        }

        /// <summary>
        /// Using path hash value to get a pointer hash value from PointerStore.
        /// The pointer hash value represents a actual address of database.
        /// </summary>
        /// <param name="pathHash"></param>
        /// <returns></returns>
        public async Task<Hash> GetPointer(Hash pathHash)
        {
            return await _pointerStore.GetAsync(pathHash);
        }

        /// <summary>
        /// Insert a Change to current ChangesStore.
        /// And refresh the paths count of current world state,
        /// as well as insert a changed path to DataStore.
        /// The key to get the changed path can be calculated by _preBlockHash and the order.
        /// </summary>
        /// <param name="pathHash"></param>
        /// <param name="hashBefore"></param>
        /// <param name="pointerHash"></param>
        /// <returns></returns>
        public async Task InsertChange(Hash pathHash, Hash hashBefore, Hash pointerHash)
        {
            await _changesStore.InsertAsync(pathHash, new Change()
            {
                Before = hashBefore,
                After = pointerHash
            });
            
            var countBytes = await _dataStore.GetData(GetHashToGetPathsCount());
            countBytes = countBytes ?? LongToBytes(0);
            var count = BytesToLong(countBytes);
            //Kind of important.
            var key = _preBlockHash.CombineHashReverse(countBytes);
            await _dataStore.SetData(key, pathHash.Value.ToByteArray());
            count++;
            await _dataStore.SetData(GetHashToGetPathsCount(), LongToBytes(count));
        }

        /// <summary>
        /// The normal way to get a pointer hash value from a Path instance.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public Hash GetPointer(Path path)
        {
            return path.SetBlockHash(_preBlockHash).GetPointerHash();
        }

        /// <summary>
        /// Rollback data to previous set world state
        /// by rollback the PointerStore.
        /// </summary>
        /// <returns></returns>
        public async Task RollbackDataToPreviousWorldState()
        {
            var dict = await GetChangesDictionaryAsync();
            foreach (var pair in dict)
            {
                await _pointerStore.UpdateAsync(pair.Key, pair.Value.Before);
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
        
        /// <summary>
        /// Using a pointer hash value like a key to set a byte array to DataStore.
        /// </summary>
        /// <param name="pointerHash"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task SetData(Hash pointerHash, byte[] data)
        {
            await _dataStore.SetData(pointerHash, data);
        }

        /// <summary>
        /// Using a pointer hash value to get data from DataStore.
        /// </summary>
        /// <param name="pointerHash"></param>
        /// <returns></returns>
        public async Task<byte[]> GetData(Hash pointerHash)
        {
            return await _dataStore.GetData(pointerHash);
        }

        /// <summary>
        /// A specific way to get a hash value which pointer to
        /// the count of Changes of a world state.
        /// </summary>
        /// <param name="blockHash"></param>
        /// <returns></returns>
        private Hash GetHashToGetPathsCount(Hash blockHash = null)
        {
            Interlocked.CompareExchange(ref blockHash, _preBlockHash, null);
            Hash origin = "paths".CalculateHash();
            return origin.CombineHash(blockHash);
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
            var fixedPaths = new List<Hash>();
            var changedPathsCountBytes = await _dataStore.GetData(GetHashToGetPathsCount(blockHash));
            if (changedPathsCountBytes == null)
            {
                return null;
            }
            
            var changedPathsCount = BytesToLong(changedPathsCountBytes);
            for (var i = 0; i < changedPathsCount; i++)
            {
                var key = blockHash.CombineHashReverse(LongToBytes(i));
                var path = await _dataStore.GetData(key);
                fixedPaths.Add(path);
            }

            return fixedPaths;
        }

        /// <summary>
        /// Using a paths list to get Changes from a ChangesStore.
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="blockHash"></param>
        /// <returns></returns>
        public async Task<List<Change>> GetChangesAsync(Hash chainId, Hash blockHash)
        {
            var paths = await GetPathsAsync(blockHash);
            var changesStore = await _worldStateStore.GetWorldState(chainId, blockHash);
            var changes = new List<Change>();
            foreach (var path in paths)
            {
                var change = await changesStore.GetAsync(path);
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
                var change = await _changesStore.GetAsync(path);
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
            foreach (var path in paths)
            {
                var change = await _changesStore.GetAsync(path);
                dict[path] = change;
            }

            return dict;
        }

        private byte[] LongToBytes(long number)
        {
            return BitConverter.IsLittleEndian ? 
                BitConverter.GetBytes(number).Reverse().ToArray() : 
                BitConverter.GetBytes(number);
        }

        private long BytesToLong(byte[] bytes)
        {
            return BitConverter.ToInt64(
                BitConverter.IsLittleEndian ? 
                bytes.Reverse().ToArray() : 
                bytes, 0);
        }
    }
}