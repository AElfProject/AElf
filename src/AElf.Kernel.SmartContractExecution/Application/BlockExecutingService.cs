using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly ITransactionExecutingService _executingService;
        private readonly IBlockchainStateService _blockchainStateService;
        public ILocalEventBus EventBus { get; set; }
        public ILogger<BlockExecutingService> Logger { get; set; }

        public BlockExecutingService(ITransactionExecutingService executingService,
            IBlockchainStateService blockchainStateService)
        {
            _executingService = executingService;
            _blockchainStateService = blockchainStateService;
            EventBus = NullLocalEventBus.Instance;
        }

        public async Task<Block> ExecuteBlockAsync(BlockHeader blockHeader,
            IEnumerable<Transaction> nonCancellableTransactions)
        {
            return await ExecuteBlockAsync(blockHeader, nonCancellableTransactions, new List<Transaction>(),
                CancellationToken.None);
        }

        public async Task<Block> ExecuteBlockAsync(BlockHeader blockHeader,
            IEnumerable<Transaction> nonCancellableTransactions, IEnumerable<Transaction> cancellableTransactions,
            CancellationToken cancellationToken)
        {
            Logger.LogTrace("Entered ExecuteBlockAsync");
            var nonCancellable = nonCancellableTransactions.ToList();
            var cancellable = cancellableTransactions.ToList();

            var nonCancellableReturnSets =
                await _executingService.ExecuteAsync(
                    new TransactionExecutingDto {BlockHeader = blockHeader, Transactions = nonCancellable},
                    CancellationToken.None, true);
            Logger.LogTrace("Executed non-cancellable txs");

            var returnSetCollection = new ReturnSetCollection(nonCancellableReturnSets);
            List<ExecutionReturnSet> cancellableReturnSets = new List<ExecutionReturnSet>();
            if (!cancellationToken.IsCancellationRequested && cancellable.Count > 0)
            {
                cancellableReturnSets = await _executingService.ExecuteAsync(
                    new TransactionExecutingDto
                    {
                        BlockHeader = blockHeader,
                        Transactions = cancellable,
                        PartialBlockStateSet = returnSetCollection.ToBlockStateSet()
                    },
                    cancellationToken, false);
                returnSetCollection.AddRange(cancellableReturnSets);
                Logger.LogTrace("Executed cancellable txs");
            }

            if (returnSetCollection.Unexecutable.Count > 0)
            {
                await EventBus.PublishAsync(
                    new UnexecutableTransactionsFoundEvent(blockHeader, returnSetCollection.Unexecutable));
            }

            var executedCancellableTransactions = new HashSet<Hash>(cancellableReturnSets.Select(x => x.TransactionId));
            var allExecutedTransactions =
                nonCancellable.Concat(cancellable.Where(x => executedCancellableTransactions.Contains(x.GetHash())))
                    .ToList();
            var block = await FillBlockAfterExecutionAsync(blockHeader, allExecutedTransactions,
                returnSetCollection.Executed);
            return block;
        }
        
        private async Task<Block> FillBlockAfterExecutionAsync(BlockHeader blockHeader, List<Transaction> transactions,
            List<ExecutionReturnSet> blockExecutionReturnSet)
        {
            var bloom = new Bloom();
            var blockStateSet = new BlockStateSet
            {
                BlockHeight = blockHeader.Height,
                PreviousHash = blockHeader.PreviousBlockHash
            };
            foreach (var returnSet in blockExecutionReturnSet)
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

                if (returnSet.Status == TransactionResultStatus.Mined)
                {
                    bloom.Combine(new[] {new Bloom(returnSet.Bloom.ToByteArray())});    
                }
            }

            blockHeader.Bloom = ByteString.CopyFrom(bloom.Data);
            blockHeader.MerkleTreeRootOfWorldState = CalculateWorldStateMerkleTreeRoot(blockStateSet);
            
            var allExecutedTransactionIds = transactions.Select(x => x.GetHash()).ToList();
            blockExecutionReturnSet = blockExecutionReturnSet.AsParallel()
                .OrderBy(d => allExecutedTransactionIds.IndexOf(d.TransactionId)).ToList();
            blockHeader.MerkleTreeRootOfTransactionStatus =
                CalculateTransactionStatusMerkleTreeRoot(blockExecutionReturnSet);
            
            blockHeader.MerkleTreeRootOfTransactions = CalculateTransactionMerkleTreeRoot(allExecutedTransactionIds);
            
            var blockHash = blockHeader.GetHashWithoutCache();
            var blockBody = new BlockBody();
            blockBody.TransactionIds.AddRange(allExecutedTransactionIds);
            
            var block = new Block
            {
                Header = blockHeader,
                Body = blockBody
            };
            blockStateSet.BlockHash = blockHash;

            await _blockchainStateService.SetBlockStateSetAsync(blockStateSet);

            return block;
        }

        private Hash CalculateWorldStateMerkleTreeRoot(BlockStateSet blockStateSet)
        {
            Hash merkleTreeRootOfWorldState;
            var byteArrays = GetDeterministicByteArrays(blockStateSet);
            using (var hashAlgorithm = SHA256.Create())
            {
                foreach (var bytes in byteArrays)
                {
                    hashAlgorithm.TransformBlock(bytes, 0, bytes.Length, null, 0);
                }

                hashAlgorithm.TransformFinalBlock(new byte[0], 0, 0);
                merkleTreeRootOfWorldState = Hash.FromByteArray(hashAlgorithm.Hash);
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
            return BinaryMerkleTree.FromLeafNodes(transactionIds).Root;
        }
        
        private Hash GetHashCombiningTransactionAndStatus(Hash txId,
            TransactionResultStatus executionReturnStatus)
        {
            // combine tx result status
            var rawBytes = txId.ToByteArray().Concat(Encoding.UTF8.GetBytes(executionReturnStatus.ToString()))
                .ToArray();
            return Hash.FromRawBytes(rawBytes);
        }
    }
}