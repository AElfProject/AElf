using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Kernel.Storages;
using Google.Protobuf.WellKnownTypes;
using AElf.Kernel;
using AElf.Kernel.Managers;
using NLog;

// ReSharper disable CheckNamespace
namespace AElf.SmartContract
{
    [LoggerName(nameof(StateDictator))]
    public class StateDictator: IStateDictator
    {
        private readonly IDataStore _dataStore;
        private readonly ILogger _logger;
        private readonly HashManager _hashManager;
        private readonly TransactionManager _transactionManager;
        
        private WorldState _worldState;

        public Hash ChainId { get; set; }
        public Hash BlockProducerAccountAddress { get; set; }
        public ulong CurrentRoundNumber { get; set; }

        public StateDictator(HashManager hashManager, TransactionManager transactionManager, IDataStore dataStore, ILogger logger = null)
        {
            _dataStore = dataStore;
            _logger = logger;

            _hashManager = hashManager;
            _transactionManager = transactionManager;
        }

        public async Task<ulong> GetChainCurrentHeightAsync(Hash chainId)
        {
            var key = chainId.SetHashType(HashType.ChainHeight);
            var height = await _dataStore.GetAsync<UInt64Value>(key);
            return height.Value;
        }

        public async Task SetChainCurrentHeightAsync(Hash chainId, ulong height)
        {
            var key = chainId.SetHashType(HashType.ChainHeight);
            await _dataStore.InsertAsync(key, new UInt64Value
            {
                Value = height
            });
        }
        
        /// <summary>
        /// Get an AccountDataProvider instance
        /// </summary>
        /// <param name="accountAddress"></param>
        /// <returns></returns>
        public IAccountDataProvider GetAccountDataProvider(Hash accountAddress)
        {
            return new AccountDataProvider(accountAddress, this);
        }

        /// <summary>
        /// Get a WorldState instance.
        /// </summary>
        /// <param name="blockHash"></param>
        /// <returns></returns>
        public async Task<WorldState> GetWorldStateAsync(Hash blockHash)
        {
            return await _dataStore.GetAsync<WorldState>(CalculateHashFroWorldState(ChainId, blockHash));
        }

        private static Hash CalculateHashFroWorldState(Hash chainId, Hash blockHash)
        {
            return chainId.CalculateHashWith(blockHash);
        }
        
        public async Task SetWorldStateAsync(Hash stateHash)
        {
            await _dataStore.InsertAsync(stateHash, _worldState);
            _worldState = new WorldState();
        }

        public async Task<Hash> GetHashAsync(Hash hash)
        {
            return await _hashManager.GetHash(hash);
        }

        public async Task SetHashAsync(Hash origin, Hash another)
        {
            await _hashManager.SetHash(origin, another);
        }

        public async Task<Hash> GetBlockHashAsync(Hash stateHash)
        {
            return await _hashManager.GetHash(stateHash);
        }

        public async Task SetBlockHashAsync(Hash stateHash, Hash blockHash)
        {
            await _hashManager.SetHash(stateHash, blockHash);
        }

        public async Task<Hash> GetStateHashAsync(Hash blockHash)
        {
            return await _hashManager.GetHash(blockHash);
        }

        public async Task SetStateHashAsync(Hash blockHash, Hash stateHash)
        {
            await _hashManager.SetHash(blockHash, stateHash);
        }

        /// <summary>
        /// Using a pointer hash value like a key to set a byte array to DataStore.
        /// </summary>
        /// <param name="pointerHash"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task SetDataAsync(Hash pointerHash, byte[] data)
        {
            await _dataStore.InsertAsync<Hash>(pointerHash, data);
        }

        /// <summary>
        /// Using a pointer hash value to get data from DataStore.
        /// </summary>
        /// <param name="pointerHash"></param>
        /// <returns></returns>
        public async Task<byte[]> GetDataAsync(Hash pointerHash)
        {
            return await _dataStore.GetAsync(pointerHash);
        }

        public Task RollbackToPreviousBlock()
        {
            throw new NotImplementedException();
        }

        public async Task ApplyStateValueChangeAsync(StateValueChange stateValueChange)
        {
            await SetHashAsync(stateValueChange.Path.ResourcePathHash, stateValueChange.Path.ResourcePointerHash);
        }

        /// <summary>
        /// Apply data changes in cache layer into database
        /// </summary>
        /// <param name="cachedActions"></param>
        /// <returns></returns>
        public async Task<bool> ApplyCachedDataAction(Dictionary<DataPath, StateCache> cachedActions)
        {
            _logger?.Debug($"Pipeline set {cachedActions.Count} data item");
            
            //Only dirty, i.e., changed data item, will be applied to database
            var pipelineSet = cachedActions.Where(kv => kv.Value.Dirty)
                .ToDictionary(kv => new Hash(kv.Key.ResourcePointerHash), kv => kv.Value.CurrentValue);
            if (pipelineSet.Count > 0)
            {
                //_logger?.Debug($"Pipeline set {pipelineSet.Count} data item");
                return await _dataStore.PipelineSetDataAsync(pipelineSet);
            }

            //return true for read-only
            return true;
        }
    }
}