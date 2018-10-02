using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Kernel.Storages;
using Google.Protobuf.WellKnownTypes;
using AElf.Kernel;
using AElf.Kernel.Managers;
using Google.Protobuf;
using Mono.Cecil;
using NLog;
using AElf.Common;

// ReSharper disable CheckNamespace
namespace AElf.SmartContract
{
    [LoggerName(nameof(StateDictator))]
    public class StateDictator: IStateDictator
    {
        private readonly IDataStore _dataStore;
        private readonly ILogger _logger;
        private readonly IHashManager _hashManager;
        private readonly ITransactionManager _transactionManager;

        private WorldState _worldState = new WorldState();

        public Hash ChainId { get; set; }
        public Address BlockProducerAccountAddress { get; set; } = Address.Zero;
        public ulong BlockHeight { get; set; }

        public StateDictator(IHashManager hashManager, ITransactionManager transactionManager, IDataStore dataStore, ILogger logger = null)
        {
            _dataStore = dataStore;
            _logger = logger;

            _hashManager = hashManager;
            _transactionManager = transactionManager;
        }

        
        /// <summary>
        /// Get an AccountDataProvider instance
        /// </summary>
        /// <param name="accountAddress"></param>
        /// <returns></returns>
        public IAccountDataProvider GetAccountDataProvider(Address accountAddress)
        {
            return new AccountDataProvider(accountAddress, this);
        }

        /// <summary>
        /// Get a WorldState instance.
        /// </summary>
        /// <param name="stateHash"></param>
        /// <returns></returns>
        public async Task<WorldState> GetWorldStateAsync(Hash stateHash)
        {
            return await _dataStore.GetAsync<WorldState>(stateHash);
        }
        
        public async Task<WorldState> GetLatestWorldStateAsync()
        {            
            var dataPath = new DataPath
            {
                ChainId = ChainId,
                BlockHeight = BlockHeight,
                BlockProducerAddress = BlockProducerAccountAddress
            };
            return await _dataStore.GetAsync<WorldState>(dataPath.StateHash);
        }

        public async Task SetWorldStateAsync()
        {
            var dataPath = new DataPath
            {
                ChainId = ChainId,
                BlockHeight = BlockHeight,
                BlockProducerAddress = BlockProducerAccountAddress
            };
            await _dataStore.InsertAsync(dataPath.StateHash, _worldState);
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
            return await _hashManager.GetHash(stateHash.OfType(HashType.StateHash));
        }

        public async Task SetBlockHashAsync(Hash blockHash)
        {
            var dataPath = new DataPath
            {
                ChainId = ChainId,
                BlockHeight = BlockHeight,
                BlockProducerAddress = BlockProducerAccountAddress
            };
            await _hashManager.SetHash(dataPath.StateHash, blockHash.OfType(HashType.BlockHash));
        }

        public async Task<Hash> GetStateHashAsync(Hash blockHash)
        {
            return await _hashManager.GetHash(blockHash.OfType(HashType.BlockHash));
        }

        public async Task SetStateHashAsync(Hash blockHash)
        {
            var dataPath = new DataPath
            {
                ChainId = ChainId,
                BlockHeight = BlockHeight,
                BlockProducerAddress = BlockProducerAccountAddress
            };
            await _hashManager.SetHash(blockHash.OfType(HashType.BlockHash), dataPath.StateHash.OfType(HashType.StateHash));
        }

        /// <summary>
        /// Using a pointer hash value like a key to set a byte array to DataStore.
        /// </summary>
        /// <param name="pointerHash"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task SetDataAsync<T>(Hash pointerHash, T data) where T : IMessage
        {
            await _dataStore.InsertAsync(pointerHash, data);
        }

        /// <summary>
        /// Using a pointer hash value to get data from DataStore.
        /// </summary>
        /// <param name="pointerHash"></param>
        /// <returns></returns>
        public async Task<T> GetDataAsync<T>(Hash pointerHash) where T : IMessage, new()
        {
            return await _dataStore.GetAsync<T>(pointerHash);
        }

        public Task RollbackToPreviousBlock()
        {
            _worldState = new WorldState();
            return Task.CompletedTask;
        }

        public async Task ApplyStateValueChangeAsync(StateValueChange stateValueChange)
        {
            await SetHashAsync(stateValueChange.Path.ResourcePathHash, stateValueChange.Path.ResourcePointerHash);
            
            var dataItem = new DataItem
            {
                ResourcePath = stateValueChange.Path.ResourcePathHash,
                ResourcePointer = stateValueChange.Path.ResourcePointerHash,
                StateMerkleTreeLeaf = CalculateMerkleTreeLeaf(stateValueChange)
            };
            
            _worldState.Data.Add(dataItem);
        }

        private Hash CalculateMerkleTreeLeaf(StateValueChange stateValueChange)
        {
            var data = stateValueChange.CurrentValue;
            var length = data.Length / 3;
            var represent = stateValueChange.ToString().Substring(length, data.Length > 150 ? 50 : length);
            return HashExtensions.CalculateHashOfHashList(
                stateValueChange.Path.ResourcePathHash,
                Hash.FromString(represent)
            );
        }

        /// <summary>
        /// Apply data changes in cache layer into database
        /// </summary>
        /// <param name="cachedActions"></param>
        /// <returns></returns>
        public async Task<bool> ApplyCachedDataAction(Dictionary<DataPath, StateCache> cachedActions)
        {
            _logger?.Debug($"Pipeline set {cachedActions.Count} data item");

            var pipelineSet = cachedActions.ToDictionary(kv => kv.Key.Key, kv => kv.Value.CurrentValue);
            if (pipelineSet.Count > 0)
            {
                return await _dataStore.PipelineSetDataAsync(pipelineSet);
            }

            //return true for read-only
            return true;
        }
    }
}