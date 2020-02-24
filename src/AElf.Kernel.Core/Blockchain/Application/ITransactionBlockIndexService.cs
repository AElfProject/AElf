using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Domain;
using AElf.Types;
using Google.Protobuf.Collections;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Blockchain.Application
{
    //TODO: we need a meeting to talk about the this class how to deal with the fork, and the performance problem
    public interface ITransactionBlockIndexService
    {
        Task<BlockIndex> GetTransactionBlockIndexAsync(Hash txId);
        Task UpdateTransactionBlockIndexAsync(IList<Hash> txIds, BlockIndex blockIndex);
        Task<BlockIndex> GetCachedTransactionBlockIndexAsync(Hash txId, Hash chainBranchBlockHash = null);
        Task InitializeTransactionBlockIndexCacheAsync();
        Task CleanTransactionBlockIndexCacheAsync(long blockHeight);
    }

    public class TransactionBlockIndexService : ITransactionBlockIndexService, ITransientDependency
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionBlockIndexManager _transactionBlockIndexManager;

        public TransactionBlockIndexService(IBlockchainService blockchainService,
            ITransactionBlockIndexManager transactionBlockIndexManager)
        {
            _blockchainService = blockchainService;
            _transactionBlockIndexManager = transactionBlockIndexManager;
        }

        public async Task<BlockIndex> GetTransactionBlockIndexAsync(Hash txId)
        {
            var transactionBlockIndex = await _transactionBlockIndexManager.GetTransactionBlockIndexAsync(txId);

            if (transactionBlockIndex == null)
                return null;

            return await GetBlockIndexAsync(transactionBlockIndex);
        }

        public async Task UpdateTransactionBlockIndexAsync(IList<Hash> txIds, BlockIndex blockIndex)
        {
            var transactionBlockIndexes = new Dictionary<Hash, TransactionBlockIndex>();
            foreach (var txId in txIds)
            {
                var preTransactionBlockIndex =
                    await _transactionBlockIndexManager.GetTransactionBlockIndexAsync(txId);

                var transactionBlockIndex = new TransactionBlockIndex
                {
                    BlockHash = blockIndex.BlockHash,
                    BlockHeight = blockIndex.BlockHeight
                };

                if (preTransactionBlockIndex != null)
                {
                    if (preTransactionBlockIndex.BlockHash.Equals(blockIndex.BlockHash) ||
                        preTransactionBlockIndex.PreviousExecutionBlockIndexList.Count(l =>
                            l.BlockHash.Equals(blockIndex.BlockHash)) != 0)
                    {
                        return;
                    }

                    transactionBlockIndex.PreviousExecutionBlockIndexList.Add(preTransactionBlockIndex
                        .PreviousExecutionBlockIndexList);
                    var previousBlockIndex = new BlockIndex(preTransactionBlockIndex.BlockHash,
                        preTransactionBlockIndex.BlockHeight);
                    transactionBlockIndex.PreviousExecutionBlockIndexList.Add(previousBlockIndex);
                }

                transactionBlockIndexes.Add(txId, transactionBlockIndex);
            }

            await _transactionBlockIndexManager.SetTransactionBlockIndexesAsync(transactionBlockIndexes);
        }

        public async Task<BlockIndex> GetCachedTransactionBlockIndexAsync(Hash txId, Hash chainBranchBlockHash = null)
        {
            var transactionBlockIndex = await _transactionBlockIndexManager.GetCachedTransactionBlockIndexAsync(txId);

            if (transactionBlockIndex == null)
                return null;

            return await GetBlockIndexAsync(transactionBlockIndex, chainBranchBlockHash);
        }

        public async Task InitializeTransactionBlockIndexCacheAsync()
        {
            var chain = await _blockchainService.GetChainAsync();

            if (chain == null)
                return;

            var blockHeight = chain.LastIrreversibleBlockHeight;
            var blockHash = chain.LastIrreversibleBlockHash;
            while (true)
            {
                var block = await _blockchainService.GetBlockByHashAsync(blockHash);
                if(block == null)
                    return;
                
                foreach (var txId in block.TransactionIds)
                {
                    await _transactionBlockIndexManager.GetTransactionBlockIndexAsync(txId);
                }

                if (blockHeight == Constants.GenesisBlockHeight || blockHeight <=
                    chain.LastIrreversibleBlockHeight - KernelConstants.ReferenceBlockValidPeriod +1)
                    break;

                blockHash = block.Header.PreviousBlockHash;
                blockHeight--;
            }
        }

        public Task CleanTransactionBlockIndexCacheAsync(long blockHeight)
        {
            var cleanHeight = blockHeight - KernelConstants.ReferenceBlockValidPeriod;
            if (cleanHeight > 0)
                _transactionBlockIndexManager.CleanTransactionBlockIndexCacheAsync(cleanHeight);

            return Task.CompletedTask;
        }

        private async Task<BlockIndex> GetBlockIndexAsync(TransactionBlockIndex transactionBlockIndex,
            Hash chainBranchBlockHash = null)
        {
            var chain = await _blockchainService.GetChainAsync();
            if (chainBranchBlockHash == null)
                chainBranchBlockHash = chain.BestChainHash;

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
    }
}