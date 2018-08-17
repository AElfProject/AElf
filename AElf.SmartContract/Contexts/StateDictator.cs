using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Kernel.Storages;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AElf.Kernel;
using NLog;

// ReSharper disable CheckNamespace
namespace AElf.SmartContract
{
    [LoggerName(nameof(StateDictator))]
    public class StateDictator: IStateDictator
    {
        private readonly Hash _chainId;
        private readonly IDataStore _dataStore;
        private readonly ILogger _logger;

        private WorldState _worldState;

        public Hash BlockProducerAccountAddress { get; set; }
        public ulong CurrentRoundNumber { get; set; }

        public StateDictator(Hash chainId, IDataStore dataStore, ILogger logger = null)
        {
            _chainId = chainId;
            _dataStore = dataStore;
            _logger = logger;
        }
        
        /// <summary>
        /// Rollback the state to previous block,
        /// always happen during the mining (before setting world state).
        /// </summary>
        /// <returns></returns>
        public async Task RollbackToPreviousBlock()
        {
            foreach (var data in _worldState.GetContext())
            {
                await UpdatePointerAsync(data.ResourcePath, data.ResourcePointer);
            }
        }

        public async Task RollbackToBlockHash(Hash blockHash)
        {
            await RollbackToPreviousBlock();
        }
        
        /// <summary>
        /// The world state will rollback to specific block height's world state
        /// It means world state of that height will be kept
        /// </summary>
        /// <param name="specificHeight"></param>
        /// <returns></returns>
        public async Task<List<Transaction>> RollbackToSpecificHeight(ulong specificHeight)
        {
            if (specificHeight < 1)
            {
                throw new InvalidOperationException("Cannot only rollback world state to height greater than 0");
            }
            
            await RollbackToPreviousBlock();
            
            var currentHeight = await GetChainCurrentHeight(_chainId);
            
            _logger?.Trace($"Rollback start. Current height: {currentHeight}");

            //Update the height of current chain
            await SetChainCurrentHeight(_chainId, specificHeight);

            //Update last block hash of curent chain
            var lastBlockHash = ResourcePath.CalculatePointerForGettingBlockHashByHeight(_chainId, specificHeight - 1);

            var txs = await RollbackTxs(currentHeight, specificHeight);
            
            _logger?.Trace($"Already rollback to height: {await GetChainCurrentHeight(_chainId)}");
            
            await RollbackToPreviousBlock();

            return txs;
        }

        private async Task<List<Transaction>> RollbackTxs(ulong currentHeight, ulong specificHeight)
        {
            var txs = new List<Transaction>();
            for (var i = currentHeight - 1; i >= specificHeight; i--)
            {
                var rollBackBlockHash =
                    await _dataStore.GetAsync<Hash>(
                        ResourcePath.CalculatePointerForGettingBlockHashByHeight(_chainId, i));
                var header = await _dataStore.GetAsync<BlockHeader>(rollBackBlockHash);
                var body = await _dataStore.GetAsync<BlockBody>(header.GetHash().CalculateHashWith(header.MerkleTreeRootOfTransactions));
                foreach (var txId in body.Transactions)
                {
                    var tx = await _dataStore.GetAsync<Transaction>(txId);
                    if (tx == null)
                    {
                        _logger?.Trace($"tx {txId} is null");
                    }
                    txs.Add(tx);
                    await _dataStore.RemoveAsync<Transaction>(txId);
                }

                _logger?.Trace(
                    $"Rollback block hash: " +
                    $"{rollBackBlockHash.Value.ToByteArray().ToHex()}");
            }

            return txs;
        }
        
        private async Task<ulong> GetChainCurrentHeight(Hash chainId)
        {
            var key = ResourcePath.CalculatePointerForCurrentBlockHeight(chainId);
            var height = await _dataStore.GetAsync<UInt64Value>(key);
            return height.Value;
        }
        
        public async Task SetChainCurrentHeight(Hash chainId, ulong height)
        {
            var key = ResourcePath.CalculatePointerForCurrentBlockHeight(chainId);
            await _dataStore.InsertAsync(key, new UInt64Value
            {
                Value = height
            });
        }
        
        public async Task<Hash> GetChainLastBlockHash(Hash chainId)
        {
            var key = ResourcePath.CalculatePointerForLastBlockHash(chainId);
            return await _dataStore.GetAsync<Hash>(key);
        }

        /// <summary>
        /// Get an AccountDataProvider instance
        /// </summary>
        /// <param name="accountAddress"></param>
        /// <returns></returns>
        public async Task<IAccountDataProvider> GetAccountDataProvider(Hash accountAddress)
        {
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
            return await _dataStore.GetAsync<WorldState>(CalculateHashFroWorldState(_chainId, blockHash));
        }

        private Hash CalculateHashFroWorldState(Hash chainId, Hash blockHash)
        {
            return chainId.CalculateHashWith(blockHash);
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
            
            await _dataStore.InsertAsync(CalculateHashFroWorldState(_chainId, PreBlockHash), _worldState);
            
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
            throw new NotImplementedException();
        }
        /// <summary>
        /// Using path hash value to get a pointer hash value from PointerStore.
        /// The pointer hash value represents a actual address of database.
        /// </summary>
        /// <param name="pathHash"></param>
        /// <returns></returns>
        public async Task<Hash> GetPointerAsync(Hash pathHash)
        {
            return await _dataStore.GetAsync<Hash>(pathHash);
        }
        #endregion

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

        /// <summary>
        /// The normal way to get a pointer hash value from a Path instance.
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <returns></returns>
        public async Task<Hash> CalculatePointerHashOfCurrentHeight(IResourcePath resourcePath)
        {
            return resourcePath.SetBlockProducerAddress(BlockProducerAccountAddress)
                .SetBlockHash(PreBlockHash).GetPointerHash();
        }

        public async Task ApplyStateValueChangeAsync(StateValueChange stateValueChange, Hash chainId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Apply data changes in cache layer into database
        /// </summary>
        /// <param name="cachedActions"></param>
        /// <param name="chainId"></param>
        /// <returns></returns>
        public async Task<bool> ApplyCachedDataAction(Dictionary<Hash, StateCache> cachedActions, Hash chainId)
        {
            Hash prevBlockHash = await _dataStore.GetAsync<Hash>(ResourcePath.CalculatePointerForLastBlockHash(chainId));
            
            _logger?.Debug($"Pipeline set {cachedActions.Count} data item");
            
            //Only dirty, i.e., changed data item, will be applied to database
            var pipelineSet = cachedActions.Where(kv => kv.Value.Dirty)
                .ToDictionary(kv => new Hash(kv.Key.CalculateHashWith(prevBlockHash)), kv => kv.Value.CurrentValue);
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