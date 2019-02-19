using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;

namespace AElf.Execution.Execution
{
    public class BlockExecutingService : IBlockExecutingService
    {
        private readonly ITransactionExecutingService _executingService;
        private readonly IBlockManager _blockManager;
        private readonly IBlockchainStateManager _blockchainStateManager;

        public BlockExecutingService(ITransactionExecutingService executingService, IBlockManager blockManager,
            IBlockchainStateManager blockchainStateManager)
        {
            _executingService = executingService;
            _blockManager = blockManager;
            _blockchainStateManager = blockchainStateManager;
        }

        public async Task ExecuteBlockAsync(int chainId, Hash blockHash)
        {
            // TODO: If already executed, don't execute again. Maybe check blockStateSet?
            var block = await _blockManager.GetBlockAsync(blockHash);
            var readyTxs = block.Body.TransactionList.ToList();

            // TODO: Use BlockStateSet to calculate merkle tree

            var blockStateSet = new BlockStateSet()
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Header.Height,
                PreviousHash = block.Header.PreviousBlockHash
            };

            var chainContext = new ChainContext()
            {
                ChainId = block.Header.ChainId,
                BlockHash = block.Header.PreviousBlockHash,
                BlockHeight = block.Header.Height - 1
            };
            var returnSets = await _executingService.ExecuteAsync(chainId, chainContext, readyTxs,
                block.Header.Time.ToDateTime(), CancellationToken.None);
            foreach (var returnSet in returnSets)
            {
                foreach (var change in returnSet.StateChanges)
                {
                    blockStateSet.Changes.Add(change.Key, change.Value);
                }
            }

            await _blockchainStateManager.SetBlockStateSetAsync(blockStateSet);

            // TODO: Insert deferredTransactions to TxPool
        }

        public async Task<Block> ExecuteBlockAsync(int chainId, BlockHeader blockHeader,
            IEnumerable<Transaction> nonCancellableTransactions)
        {
            return await ExecuteBlockAsync(chainId, blockHeader, nonCancellableTransactions, new List<Transaction>(),
                CancellationToken.None);
        }

        public async Task<Block> ExecuteBlockAsync(int chainId, BlockHeader blockHeader,
            IEnumerable<Transaction> nonCancellableTransactions, IEnumerable<Transaction> cancellableTransactions,
            CancellationToken cancellationToken)
        {
            // TODO: If already executed, don't execute again. Maybe check blockStateSet?

            var nonCancellable = nonCancellableTransactions.ToList();
            var cancellable = cancellableTransactions.ToList();

            var chainContext = new ChainContext()
            {
                ChainId = blockHeader.ChainId,
                BlockHash = blockHeader.PreviousBlockHash,
                BlockHeight = blockHeader.Height - 1
            };

            var blockStateSet = new BlockStateSet()
            {
                BlockHeight = blockHeader.Height,
                PreviousHash = blockHeader.PreviousBlockHash
            };
            var nonCancellableReturnSets = await _executingService.ExecuteAsync(chainId, chainContext, nonCancellable,
                blockHeader.Time.ToDateTime(), CancellationToken.None);
            var cancellableReturnSets = await _executingService.ExecuteAsync(chainId, chainContext, cancellable,
                blockHeader.Time.ToDateTime(), cancellationToken);

            foreach (var returnSet in nonCancellableReturnSets)
            {
                foreach (var change in returnSet.StateChanges)
                {
                    blockStateSet.Changes.Add(change.Key, change.Value);
                }
            }

            foreach (var returnSet in cancellableReturnSets)
            {
                foreach (var change in returnSet.StateChanges)
                {
                    blockStateSet.Changes.Add(change.Key, change.Value);
                }
            }

            // TODO: Insert deferredTransactions to TxPool

            var executed = new HashSet<Hash>(cancellableReturnSets.Select(x => x.TransactionId));
            var allExecutedTransactions = nonCancellable.Select(x => x.GetHash())
                .Concat(cancellable.Select(x => x.GetHash()).Where(x => executed.Contains(x))).ToList();
            var bmt = new BinaryMerkleTree();
            bmt.AddNodes(allExecutedTransactions);
            blockHeader.MerkleTreeRootOfTransactions = bmt.ComputeRootHash();
            blockHeader.MerkleTreeRootOfWorldState = ComputeHash(GetDeterministicByteArrays(blockStateSet));
            blockStateSet.BlockHash = blockHeader.GetHash();
            await _blockchainStateManager.SetBlockStateSetAsync(blockStateSet);
            var blockBody = new BlockBody()
            {
                BlockHeader = blockHeader.GetHash()
            };
            blockBody.Transactions.AddRange(allExecutedTransactions);
            return new Block()
            {
                Header = blockHeader,
                Body = blockBody
            };
        }

        private IEnumerable<byte[]> GetDeterministicByteArrays(BlockStateSet blockStateSet)
        {
            var keys = blockStateSet.Changes.Keys;
            foreach (var k in new SortedSet<string>(keys))
            {
                yield return Encoding.UTF8.GetBytes(k);
                yield return blockStateSet.Changes[k].ToByteArray();
            }
        }

        private Hash ComputeHash(IEnumerable<byte[]> byteArrays)
        {
            using (var hashAlgorithm = SHA256.Create())
            {
                foreach (var bytes in byteArrays)
                {
                    hashAlgorithm.TransformBlock(bytes, 0, bytes.Length, null, 0);
                }
                hashAlgorithm.TransformFinalBlock(new byte[0], 0, 0);                
                return Hash.LoadByteArray(hashAlgorithm.Hash);
            }
        }
    }
}