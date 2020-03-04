using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IBlockchainStateService
    {
        Task MergeBlockStateAsync(long lastIrreversibleBlockHeight, Hash lastIrreversibleBlockHash);
        
        Task SetBlockStateSetAsync(BlockStateSet blockStateSet);

        Task RemoveBlockStateSetsAsync(IList<Hash> blockStateHashes);
        Task<TEntity> GetBlockExecutedDataAsync<TEntity>(IChainContext chainContext);

        Task<TEntity> GetBlockExecutedDataAsync<TKey, TEntity>(IChainContext chainContext, TKey key);
        
        Task AddBlockExecutedDataAsync<TEntity>(Hash blockHash, TEntity blockExecutedData);
        
        Task AddBlockExecutedDataAsync<TKey, TEntity>(Hash blockHash, TKey key, TEntity value);
        
        Task AddBlockExecutedDataAsync<TKey, TEntity>(Hash blockHash, IDictionary<TKey, TEntity> blockExecutedData);
    }

    public class BlockchainStateService : IBlockchainStateService
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockchainStateManager _blockchainStateManager;
        public ILogger<BlockchainStateService> Logger { get; set; }

        public BlockchainStateService(IBlockchainService blockchainService,
            IBlockchainStateManager blockchainStateManager)
        {
            _blockchainService = blockchainService;
            _blockchainStateManager = blockchainStateManager;
            Logger = NullLogger<BlockchainStateService>.Instance;
        }

        public async Task MergeBlockStateAsync(long lastIrreversibleBlockHeight, Hash lastIrreversibleBlockHash)
        {
            var chainStateInfo = await _blockchainStateManager.GetChainStateInfoAsync();
            var firstHeightToMerge = chainStateInfo.BlockHeight == 0L
                ? Constants.GenesisBlockHeight
                : chainStateInfo.BlockHeight + 1;
            var mergeCount = lastIrreversibleBlockHeight - firstHeightToMerge;
            if (mergeCount < 0)
            {
                Logger.LogWarning(
                    $"Last merge height: {chainStateInfo.BlockHeight}, lib height: {lastIrreversibleBlockHeight}, needn't merge");
                return;
            }

            var blockIndexes = new List<IBlockIndex>();
            if (chainStateInfo.Status == ChainStateMergingStatus.Merged)
            {
                blockIndexes.Add(new BlockIndex(chainStateInfo.MergingBlockHash, -1));
            }

            var reversedBlockIndexes = await _blockchainService.GetReversedBlockIndexes(lastIrreversibleBlockHash, (int) mergeCount);
            reversedBlockIndexes.Reverse();
            
            blockIndexes.AddRange(reversedBlockIndexes);

            blockIndexes.Add(new BlockIndex(lastIrreversibleBlockHash, lastIrreversibleBlockHeight));

            Logger.LogDebug(
                $"Start merge lib height: {lastIrreversibleBlockHeight}, lib block hash: {lastIrreversibleBlockHash}, merge count: {blockIndexes.Count}");

            foreach (var blockIndex in blockIndexes)
            {
                try
                {
                    Logger.LogTrace($"Merging state {chainStateInfo} for block {blockIndex}");
                    await _blockchainStateManager.MergeBlockStateAsync(chainStateInfo, blockIndex.BlockHash);
                }
                catch (Exception e)
                {
                    Logger.LogError(e,
                        $"Exception while merge state {chainStateInfo} for block {blockIndex}");
                    throw;
                }
            }
        }

        public async Task SetBlockStateSetAsync(BlockStateSet blockStateSet)
        {
            await _blockchainStateManager.SetBlockStateSetAsync(blockStateSet);
        }
        
        public async Task RemoveBlockStateSetsAsync(IList<Hash> blockStateHashes)
        {
            await _blockchainStateManager.RemoveBlockStateSetsAsync(blockStateHashes);
        }
        
        public async Task<TEntity> GetBlockExecutedDataAsync<TEntity>(IChainContext chainContext)
        {
            var byteString = await _blockchainStateManager.GetStateAsync(typeof(TEntity).Name, chainContext.BlockHeight,
                chainContext.BlockHash);
            return SerializationHelper.Deserialize<TEntity>(byteString?.ToByteArray());
        }

        public async Task<TEntity> GetBlockExecutedDataAsync<TKey, TEntity>(IChainContext chainContext, TKey key)
        {
            var blockExecutedDataKey = GetBlockExecutedCacheKey<TKey, TEntity>(key);
            var byteString = await _blockchainStateManager.GetStateAsync(blockExecutedDataKey, chainContext.BlockHeight,
                chainContext.BlockHash);
            return SerializationHelper.Deserialize<TEntity>(byteString?.ToByteArray());
        }
        
        public async Task AddBlockExecutedDataAsync<TEntity>(Hash blockHash, TEntity blockExecutedData)
        {
            var dic = new Dictionary<string, ByteString>
            {
                {typeof(TEntity).Name, ByteString.CopyFrom(SerializationHelper.Serialize(blockExecutedData))}
            };
            await _blockchainStateManager.AddBlockExecutedCacheAsync(blockHash, dic);
        }

        public async Task AddBlockExecutedDataAsync<TKey, TEntity>(Hash blockHash, TKey key, TEntity blockExecutedData)
        {
            var dic = new Dictionary<string, ByteString>
            {
                {
                    GetBlockExecutedCacheKey<TKey, TEntity>(key),
                    ByteString.CopyFrom(SerializationHelper.Serialize(blockExecutedData))
                }
            };
            await _blockchainStateManager.AddBlockExecutedCacheAsync(blockHash, dic);
        }

        public async Task AddBlockExecutedDataAsync<TKey, TEntity>(Hash blockHash,
            IDictionary<TKey, TEntity> blockExecutedData)
        {
            var dic = blockExecutedData.ToDictionary(
                keyPair => GetBlockExecutedCacheKey<TKey, TEntity>(keyPair.Key),
                keyPair => ByteString.CopyFrom(SerializationHelper.Serialize(keyPair.Value)));
            await _blockchainStateManager.AddBlockExecutedCacheAsync(blockHash, dic);
        }

        private string GetBlockExecutedCacheKey<TKey, TEntity>(TKey key)
        {
            var typeName = typeof(TEntity).Name;
            return string.Join("/", typeName, key.ToString());
        }
    }
}