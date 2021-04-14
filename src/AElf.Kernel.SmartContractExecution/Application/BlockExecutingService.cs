using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AElf.CSharp.Core.Utils;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContractExecution.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class BlockExecutingService : IBlockExecutingService, ITransientDependency
    {
        private readonly ITransactionExecutingService _transactionExecutingService;
        private readonly IBlockchainStateService _blockchainStateService;
        private readonly ITransactionResultService _transactionResultService;
        private readonly ISystemTransactionExtraDataProvider _systemTransactionExtraDataProvider;
        private readonly IExecutedTransactionResultCacheProvider _executedTransactionResultCacheProvider;
        public ILocalEventBus EventBus { get; set; }
        public ILogger<BlockExecutingService> Logger { get; set; }

        public BlockExecutingService(ITransactionExecutingService transactionExecutingService,
            IBlockchainStateService blockchainStateService,
            ITransactionResultService transactionResultService,
            ISystemTransactionExtraDataProvider systemTransactionExtraDataProvider,
            IExecutedTransactionResultCacheProvider executedTransactionResultCacheProvider)
        {
            _transactionExecutingService = transactionExecutingService;
            _blockchainStateService = blockchainStateService;
            _transactionResultService = transactionResultService;
            _systemTransactionExtraDataProvider = systemTransactionExtraDataProvider;
            _executedTransactionResultCacheProvider = executedTransactionResultCacheProvider;
            EventBus = NullLocalEventBus.Instance;
        }

        public async Task<BlockExecutedSet> ExecuteBlockAsync(BlockHeader blockHeader,
            List<Transaction> nonCancellableTransactions)
        {
            var systemTransactionCount = 1;
            return await ExecuteBlockAsync(blockHeader, nonCancellableTransactions.Take(systemTransactionCount),
                nonCancellableTransactions.Skip(systemTransactionCount),
                CancellationToken.None);
        }

        public async Task<BlockExecutedSet> ExecuteBlockAsync(BlockHeader blockHeader,
            IEnumerable<Transaction> nonCancellableTransactions, IEnumerable<Transaction> cancellableTransactions,
            CancellationToken cancellationToken)
        {
            Logger.LogTrace("Begin BlockExecutingService.ExecuteBlockAsync");
            var nonCancellable = nonCancellableTransactions.ToList();
            var cancellable = cancellableTransactions.ToList();
            var nonCancellableReturnSets =
                await _transactionExecutingService.ExecuteAsync(
                    new TransactionExecutingDto {BlockHeader = blockHeader, Transactions = nonCancellable},
                    CancellationToken.None);

            var returnSetCollection = new ExecutionReturnSetCollection(nonCancellableReturnSets);
            List<ExecutionReturnSet> cancellableReturnSets = new List<ExecutionReturnSet>();

            if (!cancellationToken.IsCancellationRequested && cancellable.Count > 0)
            {
                cancellableReturnSets = await _transactionExecutingService.ExecuteAsync(
                    new TransactionExecutingDto
                    {
                        BlockHeader = blockHeader,
                        Transactions = cancellable,
                        PartialBlockStateSet = returnSetCollection.ToBlockStateSet()
                    },
                    cancellationToken);
                returnSetCollection.AddRange(cancellableReturnSets);
            }

            Logger.LogTrace("Create ExecutedCancellableTransactions HashSet");
            var executedCancellableTransactions = new HashSet<Hash>(cancellableReturnSets.Select(x => x.TransactionId));
            Logger.LogTrace("Concat AllExecutedTransactions");
            var allExecutedTransactions =
                nonCancellable.Concat(cancellable.Where(x => executedCancellableTransactions.Contains(x.GetHash())))
                    .ToList();
            var blockStateSet =
                CreateBlockStateSet(blockHeader.PreviousBlockHash, blockHeader.Height, returnSetCollection);
            var block = await FillBlockAfterExecutionAsync(blockHeader, allExecutedTransactions, returnSetCollection,
                blockStateSet);

            // set txn results
            var transactionResults = await SetTransactionResultsAsync(returnSetCollection, block.Header);

            // set blocks state
            blockStateSet.BlockHash = block.GetHash();
            await _blockchainStateService.SetBlockStateSetAsync(blockStateSet);

            // handle execution cases 
            ///await CleanUpReturnSetCollectionAsync(block.Header, returnSetCollection);

            Logger.LogTrace("End BlockExecutingService.ExecuteBlockAsync");
            return new BlockExecutedSet
            {
                Block = block,
                //TransactionMap = allExecutedTransactions.ToDictionary(p => p.GetHash(), p => p),
                TransactionResults = transactionResults
            };
        }

        private Task<Block> FillBlockAfterExecutionAsync(BlockHeader header,
            IEnumerable<Transaction> transactions, ExecutionReturnSetCollection executionReturnSetCollection,
            BlockStateSet blockStateSet)
        {
            Logger.LogTrace("Begin BlockExecutingService.FillBlockAfterExecutionAsync");
            var bloom = new Bloom();
            foreach (var returnSet in executionReturnSetCollection.Executed)
            {
                bloom.Combine(new[] {new Bloom(returnSet.Bloom.ToByteArray())});
            }

            Logger.LogTrace("Handle transaction ids");

            var allExecutedTransactionIds = transactions.Select(x => x.GetHash()).ToList();
            var txIndex = new Dictionary<Hash, int>();
            for (int i = 0; i < allExecutedTransactionIds.Count; i++)
            {
                txIndex[allExecutedTransactionIds[i]] = i;
            }

            Logger.LogTrace("Sort orderedReturnSets.");

            var orderedReturnSets = executionReturnSetCollection.GetExecutionReturnSetList()
                .OrderBy(d => txIndex[d.TransactionId]).ToList();
            
            Logger.LogTrace("End sort orderedReturnSets.");

            var block = new Block
            {
                Header = new BlockHeader(header)
                {
                    Bloom = ByteString.CopyFrom(bloom.Data),
                    MerkleTreeRootOfWorldState = CalculateWorldStateMerkleTreeRoot(blockStateSet),
                    MerkleTreeRootOfTransactionStatus = CalculateTransactionStatusMerkleTreeRoot(orderedReturnSets),
                    MerkleTreeRootOfTransactions = CalculateTransactionMerkleTreeRoot(allExecutedTransactionIds)
                },
                Body = new BlockBody
                {
                    TransactionIds = {allExecutedTransactionIds}
                }
            };

            Logger.LogTrace("End BlockExecutingService.FillBlockAfterExecutionAsync");
            return Task.FromResult(block);
        }

        protected virtual Task CleanUpReturnSetCollectionAsync(BlockHeader blockHeader,
            ExecutionReturnSetCollection executionReturnSetCollection)
        {
            return Task.CompletedTask;
        }

        private Hash CalculateWorldStateMerkleTreeRoot(BlockStateSet blockStateSet)
        {
            Logger.LogTrace("Begin BlockExecutingService.CalculateWorldStateMerkleTreeRoot");
            Hash merkleTreeRootOfWorldState;
            var byteArrays = GetDeterministicByteArrays(blockStateSet);
            using (var hashAlgorithm = SHA256.Create())
            {
                foreach (var bytes in byteArrays)
                {
                    hashAlgorithm.TransformBlock(bytes, 0, bytes.Length, null, 0);
                }

                hashAlgorithm.TransformFinalBlock(new byte[0], 0, 0);
                merkleTreeRootOfWorldState = Hash.LoadFromByteArray(hashAlgorithm.Hash);
            }
            Logger.LogTrace("End BlockExecutingService.CalculateWorldStateMerkleTreeRoot");

            return merkleTreeRootOfWorldState;
        }

        private IEnumerable<byte[]> GetDeterministicByteArrays(BlockStateSet blockStateSet)
        {
            var keys = blockStateSet.Changes.Keys;
            foreach (var k in new SortedSet<string>(keys))
            {
                yield return Encoding.UTF8.GetBytes(k);
                yield return blockStateSet.Changes[k].ToByteArray();
            }

            keys = blockStateSet.Deletes;
            foreach (var k in new SortedSet<string>(keys))
            {
                yield return Encoding.UTF8.GetBytes(k);
                yield return ByteString.Empty.ToByteArray();
            }
        }

        private Hash CalculateTransactionStatusMerkleTreeRoot(List<ExecutionReturnSet> blockExecutionReturnSet)
        {
            Logger.LogTrace("Begin BlockExecutingService.CalculateTransactionStatusMerkleTreeRoot");
            var executionReturnSet = blockExecutionReturnSet.Select(executionReturn =>
                (executionReturn.TransactionId, executionReturn.Status));
            var nodes = new List<Hash>();
            foreach (var (transactionId, status) in executionReturnSet)
            {
                nodes.Add(GetHashCombiningTransactionAndStatus(transactionId, status));
            }

            Logger.LogTrace("End BlockExecutingService.CalculateTransactionStatusMerkleTreeRoot");
            return BinaryMerkleTree.FromLeafNodes(nodes).Root;
        }

        private Hash CalculateTransactionMerkleTreeRoot(IEnumerable<Hash> transactionIds)
        {
            Logger.LogTrace("Start transaction merkle tree root calculation.");
            return BinaryMerkleTree.FromLeafNodes(transactionIds).Root;
        }

        private Hash GetHashCombiningTransactionAndStatus(Hash txId,
            TransactionResultStatus executionReturnStatus)
        {
            // combine tx result status
            var rawBytes = ByteArrayHelper.ConcatArrays(txId.ToByteArray(),
                EncodingHelper.EncodeUtf8(executionReturnStatus.ToString()));
            return HashHelper.ComputeFrom(rawBytes);
        }

        private BlockStateSet CreateBlockStateSet(Hash previousBlockHash, long blockHeight,
            ExecutionReturnSetCollection executionReturnSetCollection)
        {
            Logger.LogTrace("Begin BlockExecutingService.CreateBlockStateSet");
            var blockStateSet = new BlockStateSet
            {
                BlockHeight = blockHeight,
                PreviousHash = previousBlockHash
            };
            foreach (var returnSet in executionReturnSetCollection.Executed)
            {
                foreach (var change in returnSet.StateChanges)
                {
                    blockStateSet.Changes[change.Key] = change.Value;
                    blockStateSet.Deletes.Remove(change.Key);
                }

                foreach (var delete in returnSet.StateDeletes)
                {
                    blockStateSet.Deletes.AddIfNotContains(delete.Key);
                    blockStateSet.Changes.Remove(delete.Key);
                }
            }

            Logger.LogTrace("End BlockExecutingService.CreateBlockStateSet");
            return blockStateSet;
        }

        private async Task<List<TransactionResult>> SetTransactionResultsAsync(
            ExecutionReturnSetCollection executionReturnSetCollection, BlockHeader blockHeader)
        {
            //save all transaction results
            Logger.LogTrace("Begin BlockExecutingService.SetTransactionResultsAsync");
            var results = executionReturnSetCollection.GetExecutionReturnSetList()
                .Select(p =>
                {
                    p.TransactionResult.BlockHash = blockHeader.GetHash();
                    p.TransactionResult.BlockNumber = blockHeader.Height;
                    return p.TransactionResult;
                }).ToList();

            _executedTransactionResultCacheProvider.AddTransactionResults(blockHeader.GetHash(), results);
            Task.Run(async () =>
            {
                await _transactionResultService.AddTransactionResultsAsync(results, blockHeader).ConfigureAwait(false);
            });
            Logger.LogTrace("End BlockExecutingService.SetTransactionResultsAsync");
            return results;
        }
    }
}