using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
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
        public ILocalEventBus EventBus { get; set; }
        public ILogger<BlockExecutingService> Logger { get; set; }

        public BlockExecutingService(ITransactionExecutingService transactionExecutingService,
            IBlockchainStateService blockchainStateService,
            ITransactionResultService transactionResultService)
        {
            _transactionExecutingService = transactionExecutingService;
            _blockchainStateService = blockchainStateService;
            _transactionResultService = transactionResultService;
            EventBus = NullLocalEventBus.Instance;
        }

        public async Task<BlockExecutedSet> ExecuteBlockAsync(BlockHeader blockHeader,
            IEnumerable<Transaction> nonCancellableTransactions)
        {
            return await ExecuteBlockAsync(blockHeader, nonCancellableTransactions, new List<Transaction>(),
                CancellationToken.None);
        }

        public async Task<BlockExecutedSet> ExecuteBlockAsync(BlockHeader blockHeader,
            IEnumerable<Transaction> nonCancellableTransactions, IEnumerable<Transaction> cancellableTransactions,
            CancellationToken cancellationToken)
        {
            Logger.LogTrace("Entered ExecuteBlockAsync");
            var nonCancellable = nonCancellableTransactions.ToList();
            var cancellable = cancellableTransactions.ToList();
            var nonCancellableReturnSets =
                await _transactionExecutingService.ExecuteAsync(
                    new TransactionExecutingDto {BlockHeader = blockHeader, Transactions = nonCancellable},
                    CancellationToken.None);
            Logger.LogTrace("Executed non-cancellable txs");

            var returnSetCollection = new ReturnSetCollection(nonCancellableReturnSets);
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
                Logger.LogTrace("Executed cancellable txs");
            }

            var executedCancellableTransactions = new HashSet<Hash>(cancellableReturnSets.Select(x => x.TransactionId));
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
            Logger.LogTrace("Set block state set.");
            await _blockchainStateService.SetBlockStateSetAsync(blockStateSet);
            
            // handle execution cases 
            await CleanUpReturnSetCollectionAsync(block.Header, returnSetCollection);
            
            return new BlockExecutedSet
            {
                Block = block,
                TransactionMap = allExecutedTransactions.ToDictionary(p => p.GetHash(), p => p),
                TransactionResultMap = transactionResults.ToDictionary(p => p.TransactionId, p => p)
            };
        }

        private Task<Block> FillBlockAfterExecutionAsync(BlockHeader header,
            IEnumerable<Transaction> transactions, ReturnSetCollection returnSetCollection, BlockStateSet blockStateSet)
        {
            Logger.LogTrace("Start block field filling after execution.");
            var bloom = new Bloom();
            foreach (var returnSet in returnSetCollection.Executed)
            {
                bloom.Combine(new[] {new Bloom(returnSet.Bloom.ToByteArray())});
            }
            
            var allExecutedTransactionIds = transactions.Select(x => x.GetHash()).ToList();
            var orderedReturnSets = returnSetCollection.GetExecutionReturnSetList()
                .OrderBy(d => allExecutedTransactionIds.IndexOf(d.TransactionId)).ToList();

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
            
            Logger.LogTrace("Finish block field filling after execution.");
            return Task.FromResult(block);
        }

        protected virtual async Task CleanUpReturnSetCollectionAsync(BlockHeader blockHeader, ReturnSetCollection returnSetCollection)
        {
            if (returnSetCollection.Unexecutable.Count > 0)
            {
                await EventBus.PublishAsync(
                    new UnexecutableTransactionsFoundEvent(blockHeader,
                        returnSetCollection.Unexecutable.Select(rs => rs.TransactionId).ToList()));
            }
        }

        private Hash CalculateWorldStateMerkleTreeRoot(BlockStateSet blockStateSet)
        {
            Logger.LogTrace("Start world state calculation.");
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
            Logger.LogTrace("Start transaction status merkle tree root calculation.");
            var executionReturnSet = blockExecutionReturnSet.Select(executionReturn =>
                (executionReturn.TransactionId, executionReturn.Status));
            var nodes = new List<Hash>();
            foreach (var (transactionId, status) in executionReturnSet)
            {
                nodes.Add(GetHashCombiningTransactionAndStatus(transactionId, status));
            }

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
            var rawBytes = txId.ToByteArray().Concat(Encoding.UTF8.GetBytes(executionReturnStatus.ToString()))
                .ToArray();
            return HashHelper.ComputeFrom(rawBytes);
        }

        private BlockStateSet CreateBlockStateSet(Hash previousBlockHash, long blockHeight,
            ReturnSetCollection returnSetCollection)
        {
            var blockStateSet = new BlockStateSet
            {
                BlockHeight = blockHeight,
                PreviousHash = previousBlockHash
            };
            foreach (var returnSet in returnSetCollection.Executed)
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

            return blockStateSet;
        }

        private async Task<List<TransactionResult>> SetTransactionResultsAsync(ReturnSetCollection returnSetCollection, BlockHeader blockHeader)
        {
            //save all transaction results
            var results = returnSetCollection.GetExecutionReturnSetList()
                .Select(p =>
                {
                    p.TransactionResult.BlockHash = blockHeader.GetHash();
                    return p.TransactionResult;
                }).ToList();

            await _transactionResultService.AddTransactionResultsAsync(results, blockHeader);
            return results;
        }
    }
}