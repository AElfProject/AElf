﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AElf.Kernel.Storages;
using AElf.Kernel.Types;
  using Akka.Event;
  using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
  using Debug = System.Diagnostics.Debug;

namespace AElf.Kernel.Managers
{
    public class WorldStateDictator: IWorldStateDictator
    {
        #region Stores
        private readonly IWorldStateStore _worldStateStore;
        private readonly IDataStore _dataStore;
        private readonly IChangesStore _changesStore;
        #endregion
        
        private bool _isChainIdSetted;
        private Hash _chainId;

        public bool DeleteChangeBeforesImmidiately { get; set; } = false;
        
        public Hash PreBlockHash { get; set; }

        public WorldStateDictator(IWorldStateStore worldStateStore,
            IChangesStore changesStore, IDataStore dataStore)
        {
            _worldStateStore = worldStateStore;
            _changesStore = changesStore;
            _dataStore = dataStore;
        }

        public IWorldStateDictator SetChainId(Hash chainId)
        {
            _chainId = chainId;
            _isChainIdSetted = true;
            return this;
        }
        
        /// <summary>
        /// Insert a Change to ChangesStore.
        /// And refresh the paths count of current world state,
        /// as well as insert a changed path to DataStore.
        /// The key to get the changed path can be calculated by PreBlockHash and the order.
        /// </summary>
        /// <param name="pathHash"></param>
        /// <param name="change"></param>
        /// <returns></returns>
        public async Task InsertChangeAsync(Hash pathHash, Change change)
        {
            await Check();
            
            await _changesStore.InsertChangeAsync(pathHash, change);
            
            var count = new UInt64Value {Value = 0};

            var keyToGetCount = ResourcePath.CalculatePointerForPathsCount(_chainId, PreBlockHash);
            if (await _dataStore.GetDataAsync(keyToGetCount) == null)
            {
                await _dataStore.SetDataAsync(keyToGetCount, new UInt64Value {Value = 0}.ToByteArray());
            }
            
            var result = await _dataStore.GetDataAsync(keyToGetCount);
            if (result == null)
            {
                await _dataStore.SetDataAsync(keyToGetCount, new UInt64Value {Value = 0}.ToByteArray());
            }
            else
            {
                count = UInt64Value.Parser.ParseFrom(result);
            }
            
            // make a path related to its order
            var key = CalculateKeyForPath(PreBlockHash, count);
            await _dataStore.SetDataAsync(key, pathHash.Value.ToByteArray());

            // update the count of changes
            count = new UInt64Value {Value = count.Value + 1};
            await _dataStore.SetDataAsync(keyToGetCount, count.ToByteArray());
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
                if (pair.Value.Befores.Count > 0)
                {
                    await _changesStore.UpdatePointerAsync(pair.Key, pair.Value.Befores[0]);
                }
            }
        }

        /// <summary>
        /// The world state will rollback to specific block height's world state
        /// It means world state of that height will be kept
        /// </summary>
        /// <param name="specificHeight"></param>
        /// <returns></returns>
        public async Task RollbackToSpecificHeight(ulong specificHeight)
        {
            if (specificHeight < 1)
            {
                throw new InvalidOperationException("Cannot only rollback world state to height greater than 0");
            }
            
            await Check();
            
            await RollbackCurrentChangesAsync();

            //Initial height - hash map
            var heightMap = (await GetAccountDataProvider(ResourcePath.CalculatePointerForAccountZero(_chainId)))
                .GetDataProvider().GetDataProvider("HeightOfBlock");
            
            var currentHeight = await GetChainCurrentHeight(_chainId);
            
            Debug.WriteLine($"Rollback start. Current height: {currentHeight}");

            //Update the height of current chain
            await SetChainCurrentHeight(_chainId, specificHeight);

            //Update last block hash of curent chain
            var lastBlockHash = Hash.Parser.ParseFrom(await heightMap.GetAsync(
                new UInt64Value {Value = specificHeight - 1}.CalculateHash()));
            await SetChainLastBlockHash(_chainId, lastBlockHash);
            PreBlockHash = lastBlockHash;

            //Clear the hash value of blocks already rollbacked
            var rollbackHeightList = new List<UInt64Value>();
            for (var i = currentHeight - 1; i >= specificHeight; i--)
            {
                Debug.WriteLine(
                    $"Rollback block hash: " +
                    $"{Hash.Parser.ParseFrom(await heightMap.GetAsync(new UInt64Value {Value = i}.CalculateHash())).Value.ToByteArray().ToHex()}");
                rollbackHeightList.Add(new UInt64Value {Value = i});
            }
            foreach (var height in rollbackHeightList)
            {
                var heightHash = height.CalculateHash();
                //await heightMap.SetAsync(heightHash, null);
            }
            
            Debug.WriteLine($"Already rollback to height: {await GetChainCurrentHeight(_chainId)}");
            
            await RollbackCurrentChangesAsync();
        }
        
        private async Task<ulong> GetChainCurrentHeight(Hash chainId)
        {
            var key = ResourcePath.CalculatePointerForCurrentBlockHeight(chainId);
            var heightBytes = await _dataStore.GetDataAsync(key);
            return heightBytes?.ToUInt64() ?? 0;
        }
        
        public async Task SetChainCurrentHeight(Hash chainId, ulong height)
        {
            var key = ResourcePath.CalculatePointerForCurrentBlockHeight(chainId);
            await _dataStore.SetDataAsync(key, height.ToBytes());
        }
        
        public async Task<Hash> GetChainLastBlockHash(Hash chainId)
        {
            var key = ResourcePath.CalculatePointerForLastBlockHash(chainId);
            return await _dataStore.GetDataAsync(key);
        }
        
        public async Task SetChainLastBlockHash(Hash chainId, Hash blockHash)
        {
            var key = ResourcePath.CalculatePointerForLastBlockHash(chainId);
            PreBlockHash = blockHash;
            await _dataStore.SetDataAsync(key, blockHash.GetHashBytes());
        }

        /// <summary>
        /// Get an AccountDataProvider instance
        /// </summary>
        /// <param name="accountAddress"></param>
        /// <returns></returns>
        public async Task<IAccountDataProvider> GetAccountDataProvider(Hash accountAddress)
        {
            await Check();
            
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
            await Check();
            
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
            await Check();
            
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
            await _worldStateStore.InsertWorldStateAsync(_chainId, PreBlockHash, dict);
            
            //Refresh PreBlockHash after setting WorldState.
            PreBlockHash = preBlockHash;
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
            await Check();
            Interlocked.CompareExchange(ref blockHash, PreBlockHash, null);
            
            var paths = new List<Hash>();

            var changedPathsCount = await GetChangedPathsCountAsync(blockHash);
            
            for (ulong i = 0; i < changedPathsCount; i++)
            {
                var key = CalculateKeyForPath(blockHash, new UInt64Value {Value = i});
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
            await Check();

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
        /// <param name="resourcePath"></param>
        /// <returns></returns>
        public async Task<Hash> CalculatePointerHashOfCurrentHeight(ResourcePath resourcePath)
        {
            await Check();
            
            return resourcePath.SetBlockHash(PreBlockHash).GetPointerHash();
        }

        public async Task<Change> ApplyStateValueChangeAsync(StateValueChange stateValueChange, Hash chainId)
        {
            // The code chunk is copied from DataProvider

            Hash prevBlockHash = await _dataStore.GetDataAsync(ResourcePath.CalculatePointerForLastBlockHash(chainId));
            
            //Generate the new pointer hash (using previous block hash)
            var pointerHashAfter = stateValueChange.Path.CalculateHashWith(prevBlockHash);

            var change = await GetChangeAsync(stateValueChange.Path);
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
                if (DeleteChangeBeforesImmidiately || prevBlockHash != change.LatestChangedBlockHash)
                {
                    change.ClearChangeBefores();
                }

                change.UpdateHashAfter(pointerHashAfter);
            }

            change.LatestChangedBlockHash = prevBlockHash;

            await InsertChangeAsync(stateValueChange.Path, change);
            await SetDataAsync(pointerHashAfter, stateValueChange.AfterValue.ToByteArray());
            return change;
        }

        #region Private methods

        /// <summary>
        /// Get the count of changed-paths of a specific block.
        /// </summary>
        /// <param name="blockHash"></param>
        /// <returns></returns>
        private async Task<ulong> GetChangedPathsCountAsync(Hash blockHash)
        {
            await Check();
            
            var changedPathsCount = new UInt64Value {Value = 0};
            
            var keyToGetCount = ResourcePath.CalculatePointerForPathsCount(_chainId, blockHash);
            if (await _dataStore.GetDataAsync(keyToGetCount) == null)
            {
                await _dataStore.SetDataAsync(keyToGetCount, new UInt64Value {Value = 0}.ToByteArray());
            }
            
            var result = await _dataStore.GetDataAsync(keyToGetCount);
            if (result == null)
            {
                await _dataStore.SetDataAsync(keyToGetCount, new UInt64Value {Value = 0}.ToByteArray());
            }
            else
            {
                changedPathsCount = UInt64Value.Parser.ParseFrom(result);
            }
            
            return changedPathsCount.Value;
        }

        /// <summary>
        /// Just use the result hash to get the path of a specific block and a specific order of changes.
        /// </summary>
        /// <param name="blockHash"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private Hash CalculateKeyForPath(Hash blockHash, IMessage obj)
        {
            return blockHash.CombineReverseHashWith(obj.CalculateHash());
        }

        private async Task Check()
        {
            if (!_isChainIdSetted)
            {
                throw new InvalidOperationException("Should set chain id before using a WorldStateDictator");
            }

            if (PreBlockHash == null)
            {
                var hash = await _dataStore.GetDataAsync(ResourcePath.CalculatePointerForLastBlockHash(_chainId));
                PreBlockHash = hash ?? Hash.Genesis;
            }
        }
        #endregion
    }
}