using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContractExecution.Domain;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class BlockExecutingService : IBlockExecutingService, ITransientDependency
    {
        private readonly ITransactionExecutingService _executingService;
        private readonly IBlockManager _blockManager;
        private readonly IBlockchainStateManager _blockchainStateManager;
        private readonly IBlockGenerationService _blockGenerationService;
        public BlockExecutingService(ITransactionExecutingService executingService, IBlockManager blockManager,
            IBlockchainStateManager blockchainStateManager, IBlockGenerationService blockGenerationService)
        {
            _executingService = executingService;
            _blockManager = blockManager;
            _blockchainStateManager = blockchainStateManager;
            _blockGenerationService = blockGenerationService;
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
            var returnSets = await _executingService.ExecuteAsync(chainContext, readyTxs,
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
            var nonCancellableReturnSets = await _executingService.ExecuteAsync( chainContext, nonCancellable,
                blockHeader.Time.ToDateTime(), CancellationToken.None);
            var cancellableReturnSets = await _executingService.ExecuteAsync( chainContext, cancellable,
                blockHeader.Time.ToDateTime(), cancellationToken);

            foreach (var returnSet in nonCancellableReturnSets)
            {
                foreach (var change in returnSet.StateChanges)
                {
                    blockStateSet.Changes[change.Key] = change.Value;
                }
            }

            foreach (var returnSet in cancellableReturnSets)
            {
                foreach (var change in returnSet.StateChanges)
                {
                    blockStateSet.Changes[change.Key] = change.Value;
                }
            }

            IEnumerable<byte[]> bloomData =
                nonCancellableReturnSets.Where(returnSet => returnSet.Bloom != ByteString.Empty)
                    .Select(returnSet => returnSet.Bloom.ToByteArray()).Concat(cancellableReturnSets
                        .Where(returnSet => returnSet.Bloom != ByteString.Empty)
                        .Select(returnSet => returnSet.Bloom.ToByteArray()));
            // TODO: Insert deferredTransactions to TxPool

            var merklTreeRootOfWorldState = ComputeHash(GetDeterministicByteArrays(blockStateSet));
            var executed = new HashSet<Hash>(cancellableReturnSets.Select(x => x.TransactionId));
            var allExecutedTransactions = nonCancellable.Concat(cancellable.Where(x => executed.Contains(x.GetHash()))).ToList();

            var block = await _blockGenerationService.FillBlockAsync(blockHeader, allExecutedTransactions,
                merklTreeRootOfWorldState, bloomData);
            
            blockStateSet.BlockHash = blockHeader.GetHash();
            await _blockchainStateManager.SetBlockStateSetAsync(blockStateSet);
            return block;
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