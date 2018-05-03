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
        
        /// <inheritdoc />
        public async Task<IWorldState> GetWorldStateAsync(Hash chainId, Hash blockHash)
        {
            var changesStore =  await _worldStateStore.GetWorldState(chainId, blockHash);
            var paths = await GetFixedPathsAsync(blockHash);
            return new WorldState(changesStore, paths);
        }
        
        /// <inheritdoc />
        public async Task SetWorldStateToCurrentState(Hash chainId, Hash preBlockHash)
        {
            await _worldStateStore.InsertWorldState(chainId, _preBlockHash, _changesStore);
            _changesStore = new ChangesStore(new KeyValueDatabase());
            _preBlockHash = preBlockHash;
            await _dataStore.SetData(HashToGetPreBlockHash, preBlockHash.Value.ToByteArray());
        }

        public async Task UpdatePointer(Hash pathHash, Hash pointerHash)
        {
            await _pointerStore.UpdateAsync(pathHash, pointerHash);
        }

        public async Task<Hash> GetPointer(Hash pathHash)
        {
            return await _pointerStore.GetAsync(pathHash);
        }

        public async Task InsertChange(Hash pathHash, Hash hashBefore, Hash pointerHash)
        {
            var countBytes = await _dataStore.GetData(GetHashToGetPathsCount());
            countBytes = countBytes ?? LongToBytes(0);
            var count = BytesToLong(countBytes);
            
            await _changesStore.InsertAsync(pathHash, new Change()
            {
                Before = hashBefore,
                After = pointerHash
            });
            
            var key = _preBlockHash.CombineHashReverse(countBytes);
            await _dataStore.SetData(key, pathHash.Value.ToByteArray());
            count++;
            await _dataStore.SetData(GetHashToGetPathsCount(), LongToBytes(count));
        }

        public Hash GetPointer(Path path)
        {
            return path.SetBlockHash(_preBlockHash).GetPointerHash();
        }

        /// <inheritdoc />
        public async Task RollbackDataToPreviousWorldState()
        {
            var dict = await GetChangesDictionaryAsync();
            foreach (var pair in dict)
            {
                await _pointerStore.UpdateAsync(pair.Key, pair.Value.Before);
            }
        }

        /// <inheritdoc />
        public IAccountDataProvider GetAccountDataProvider(Hash chainId, Hash accountHash)
        {
            return new AccountDataProvider(accountHash, chainId, _accountContextService, this);
        }
        
        public async Task SetData(Hash pointerHash, byte[] data)
        {
            await _dataStore.SetData(pointerHash, data);
        }

        public async Task<byte[]> GetData(Hash pointerHash)
        {
            return await _dataStore.GetData(pointerHash);
        }

        public Hash GetHashToGetPathsCount(Hash blockHash = null)
        {
            Interlocked.CompareExchange(ref blockHash, _preBlockHash, null);
            Hash origin = "paths".CalculateHash();
            return origin.CombineHash(blockHash);
        }

        public async Task<List<Hash>> GetFixedPathsAsync(Hash blockHash = null)
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

        public async Task<List<Change>> GetChangesAsync(Hash chainId, Hash blockHash)
        {
            var paths = await GetFixedPathsAsync(blockHash);
            var changesStore = await _worldStateStore.GetWorldState(chainId, blockHash);
            var changes = new List<Change>();
            foreach (var path in paths)
            {
                var change = await changesStore.GetAsync(path);
                changes.Add(change);
            }

            return changes;
        }

        public async Task<List<Change>> GetChangesAsync()
        {
            var paths = await GetFixedPathsAsync();
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
        
        public async Task<Dictionary<Hash, Change>> GetChangesDictionaryAsync()
        {
            var paths = await GetFixedPathsAsync();
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