using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Types;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Blockchain.Application
{
    public interface ITransactionBlockIndexService
    {
        Task<BlockIndex> GetTransactionBlockIndexAsync(Hash txId);
        Task AddBlockIndexAsync(IList<Hash> txIds, BlockIndex blockIndex);
        Task<bool> ValidateTransactionBlockIndexExistsInBranchAsync(Hash txId, Hash chainBranchBlockHash);
        Task LoadTransactionBlockIndexAsync();
        Task UpdateTransactionBlockIndicesByLibHeightAsync(long blockHeight);
    }

    public class TransactionBlockIndexService : ITransactionBlockIndexService, ITransientDependency
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionBlockIndexManager _transactionBlockIndexManager;
        private readonly ITransactionBlockIndexProvider _transactionBlockIndexProvider;
        public ILogger<TransactionBlockIndexService> Logger { get; set; }

        public TransactionBlockIndexService(IBlockchainService blockchainService,
            ITransactionBlockIndexManager transactionBlockIndexManager,
            ITransactionBlockIndexProvider transactionBlockIndexProvider)
        {
            _blockchainService = blockchainService;
            _transactionBlockIndexManager = transactionBlockIndexManager;
            _transactionBlockIndexProvider = transactionBlockIndexProvider;
        }

        public async Task<BlockIndex> GetTransactionBlockIndexAsync(Hash txId)
        {
            var transactionBlockIndex = await GetTransactionBlockIndexByTxIdAsync(txId);

            if (transactionBlockIndex == null)
                return null;

            var chain = await _blockchainService.GetChainAsync();
            var blockIndex = await GetBlockIndexAsync(chain, transactionBlockIndex, chain.BestChainHash);
            if (blockIndex != null)
                await ClearRedundantBlockIndices(txId, transactionBlockIndex, blockIndex, chain);
            return blockIndex;
        }

        public async Task AddBlockIndexAsync(IList<Hash> txIds, BlockIndex blockIndex)
        {
            int slots = 4;
            var listlist = new List<List<Hash>>(slots);
            for (int i = 0; i < txIds.Count; i++)
            {
                int mod = i % slots;
                if (mod >= listlist.Count)
                    listlist.Add(new List<Hash>());
                listlist[mod].Add(txIds[i]);
            }

            var tasks = listlist.Select(txIdList =>
                UpdateBlockIndex(blockIndex, txIdList));
            await Task.WhenAll(tasks);
        }

        private async Task UpdateBlockIndex(BlockIndex blockIndex, IList<Hash> txIds)
        {
            var notInProvider = txIds.ToList();
            var transactionBlockIndexes = new Dictionary<Hash, TransactionBlockIndex>();

            // foreach (var txId in txIds)
            // {
            //     if (!_transactionBlockIndexProvider.TryGetTransactionBlockIndex(txId, out var transactionBlockIndex))
            //     {
            //         notInProvider.Add(txId);
            //         continue;
            //     }
            //     
            //     transactionBlockIndexes.Add(txId, UpdateBlockIndex(blockIndex, transactionBlockIndex));
            // }

            var notInProviderTbi = await _transactionBlockIndexManager.GetTransactionBlockIndexesAsync(notInProvider);

            for (int i = 0; i < notInProvider.Count; i++)
            {
                transactionBlockIndexes.Add(notInProvider[i], UpdateBlockIndex(blockIndex, notInProviderTbi[i]));
            }

            await AddTransactionBlockIndicesAsync(transactionBlockIndexes);
        }

        private TransactionBlockIndex UpdateBlockIndex(BlockIndex blockIndex, TransactionBlockIndex preTransactionBlockIndex)
        {
            var transactionBlockIndex = new TransactionBlockIndex
            {
                BlockHash = blockIndex.BlockHash,
                BlockHeight = blockIndex.BlockHeight
            };

            if (preTransactionBlockIndex != null)
            {
                if (preTransactionBlockIndex.BlockHash == blockIndex.BlockHash ||
                    preTransactionBlockIndex.PreviousExecutionBlockIndexList.Any(l =>
                        l.BlockHash == blockIndex.BlockHash))
                {
                    return preTransactionBlockIndex;
                }

                var needToReplace = preTransactionBlockIndex.BlockHeight > blockIndex.BlockHeight;
                if (needToReplace)
                {
                    transactionBlockIndex.BlockHash = preTransactionBlockIndex.BlockHash;
                    transactionBlockIndex.BlockHeight = preTransactionBlockIndex.BlockHeight;
                }

                transactionBlockIndex.PreviousExecutionBlockIndexList.Add(preTransactionBlockIndex
                    .PreviousExecutionBlockIndexList);
                transactionBlockIndex.PreviousExecutionBlockIndexList.Add(needToReplace
                    ? blockIndex
                    : new BlockIndex(preTransactionBlockIndex.BlockHash, preTransactionBlockIndex.BlockHeight));
            }

            return transactionBlockIndex;
        }


        public async Task<bool> ValidateTransactionBlockIndexExistsInBranchAsync(Hash txId, Hash chainBranchBlockHash)
        {
            if (!_transactionBlockIndexProvider.TryGetTransactionBlockIndex(txId, out var transactionBlockIndex))
                return false;
            var chain = await _blockchainService.GetChainAsync();
            return await GetBlockIndexAsync(chain, transactionBlockIndex,
                chainBranchBlockHash ?? chain.BestChainHash) != null;
        }

        public async Task LoadTransactionBlockIndexAsync()
        {
            var chain = await _blockchainService.GetChainAsync();

            if (chain == null)
                return;

            var blockHeight = chain.LastIrreversibleBlockHeight;
            var blockHash = chain.LastIrreversibleBlockHash;
            while (true)
            {
                if (blockHeight < AElfConstants.GenesisBlockHeight ||
                    blockHeight + KernelConstants.ReferenceBlockValidPeriod <= chain.LastIrreversibleBlockHeight)
                    break;

                var block = await _blockchainService.GetBlockByHashAsync(blockHash);
                if (block == null)
                    return;

                foreach (var txId in block.TransactionIds)
                {
                    var txBlockIndex = await _transactionBlockIndexManager.GetTransactionBlockIndexAsync(txId);
                    _transactionBlockIndexProvider.AddTransactionBlockIndex(txId, txBlockIndex);
                }

                blockHash = block.Header.PreviousBlockHash;
                blockHeight--;
            }
        }

        public Task UpdateTransactionBlockIndicesByLibHeightAsync(long blockHeight)
        {
            var cleanHeight = blockHeight - KernelConstants.ReferenceBlockValidPeriod;
            if (cleanHeight <= 0)
                return Task.CompletedTask;
            _transactionBlockIndexProvider.CleanByHeight(cleanHeight);
            return Task.CompletedTask;
        }

        private async Task AddTransactionBlockIndicesAsync(
            IDictionary<Hash, TransactionBlockIndex> transactionBlockIndices)
        {
            // foreach (var index in transactionBlockIndices)
            // {
            //     _transactionBlockIndexProvider.AddTransactionBlockIndex(index.Key, index.Value);
            // }

            await _transactionBlockIndexManager.SetTransactionBlockIndicesAsync(transactionBlockIndices);
        }

        private async Task<TransactionBlockIndex> GetTransactionBlockIndexByTxIdAsync(Hash txId)
        {
            if (_transactionBlockIndexProvider.TryGetTransactionBlockIndex(txId, out var transactionBlockIndex))
                return transactionBlockIndex;

            transactionBlockIndex = await _transactionBlockIndexManager.GetTransactionBlockIndexAsync(txId);
            if (transactionBlockIndex == null)
                return null;
            _transactionBlockIndexProvider.AddTransactionBlockIndex(txId, transactionBlockIndex);

            return transactionBlockIndex;
        }

        private async Task<BlockIndex> GetBlockIndexAsync(Chain chain, TransactionBlockIndex transactionBlockIndex,
            Hash chainBranchBlockHash)
        {
            var previousBlockIndexList =
                transactionBlockIndex.PreviousExecutionBlockIndexList ?? new RepeatedField<BlockIndex>();
            var lastBlockIndex = new BlockIndex(transactionBlockIndex.BlockHash, transactionBlockIndex.BlockHeight);
            var reversedBlockIndexList = previousBlockIndexList.Concat(new[] {lastBlockIndex}).Reverse().ToList();

            foreach (var blockIndex in reversedBlockIndexList)
            {
                var blockHash =
                    await _blockchainService.GetBlockHashByHeightAsync(chain, blockIndex.BlockHeight,
                        chainBranchBlockHash);

                if (blockIndex.BlockHash == blockHash)
                    // If TransactionBlockIndex exists, then read the result via TransactionBlockIndex
                    return blockIndex;
            }

            return null;
        }

        private async Task ClearRedundantBlockIndices(Hash txId, TransactionBlockIndex transactionBlockIndex,
            BlockIndex blockIndex, Chain chain)
        {
            if (blockIndex.BlockHeight > chain.LastIrreversibleBlockHeight
                || transactionBlockIndex.PreviousExecutionBlockIndexList.Count == 0)
            {
                return;
            }

            transactionBlockIndex.BlockHash = blockIndex.BlockHash;
            transactionBlockIndex.BlockHeight = blockIndex.BlockHeight;
            transactionBlockIndex.PreviousExecutionBlockIndexList.Clear();

            _transactionBlockIndexProvider.AddTransactionBlockIndex(txId, transactionBlockIndex);
            await _transactionBlockIndexManager.SetTransactionBlockIndexAsync(txId, transactionBlockIndex);
        }
    }
}