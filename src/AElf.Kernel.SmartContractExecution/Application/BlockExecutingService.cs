using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
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
        private readonly IBlockExtraDataService _blockExtraDataService;
        private readonly IBlockchainStateManager _blockchainStateManager;

        public ILocalEventBus EventBus { get; set; }
        public ILogger<BlockExecutingService> Logger { get; set; }

        public BlockExecutingService(ITransactionExecutingService executingService,
            IBlockExtraDataService blockExtraDataService, IBlockchainStateManager blockchainStateManager)
        {
            _executingService = executingService;
            _blockExtraDataService = blockExtraDataService;
            _blockchainStateManager = blockchainStateManager;
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
                await _executingService.ExecuteAsync(blockHeader, nonCancellable, CancellationToken.None, true);
            Logger.LogTrace("Executed non-cancellable txs");

            var returnSetContainer = new ReturnSetContainer(nonCancellableReturnSets);
            List<ExecutionReturnSet> cancellableReturnSets = new List<ExecutionReturnSet>();
            if (cancellable.Count > 0)
            {
                cancellableReturnSets =
                    await _executingService.ExecuteAsync(blockHeader, cancellable, cancellationToken, false,
                        returnSetContainer.ToBlockStateSet());
                returnSetContainer.AddRange(cancellableReturnSets);
            }

            Logger.LogTrace("Executed cancellable txs");

            Logger.LogTrace("Handled return set");

            if (returnSetContainer.Unexecutable.Count > 0)
            {
                await EventBus.PublishAsync(
                    new UnexecutableTransactionsFoundEvent(blockHeader, returnSetContainer.Unexecutable));
            }

            var executed = new HashSet<Hash>(cancellableReturnSets.Select(x => x.TransactionId));
            var allExecutedTransactions =
                nonCancellable.Concat(cancellable.Where(x => executed.Contains(x.GetHash()))).ToList();
            var block = await FillBlockAfterExecutionAsync(blockHeader, allExecutedTransactions,
                returnSetContainer.Executed);

            Logger.LogTrace("Filled block");

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
                }

                if (returnSet.Status == TransactionResultStatus.Mined)
                {
                    bloom.Combine(new[] {new Bloom(returnSet.Bloom.ToByteArray())});
                }
            }

            blockHeader.Bloom = ByteString.CopyFrom(bloom.Data);
            var merkleTreeRootOfWorldState = ComputeHash(GetDeterministicByteArrays(blockStateSet));
            blockHeader.MerkleTreeRootOfWorldState = merkleTreeRootOfWorldState;

            var allExecutedTransactionIds = transactions.Select(x => x.GetHash()).ToList();
            var bmt = new BinaryMerkleTree();
            bmt.AddNodes(allExecutedTransactionIds);
            blockHeader.MerkleTreeRootOfTransactions = bmt.ComputeRootHash();

            _blockExtraDataService.FillMerkleTreeRootExtraDataForTransactionStatus(blockHeader,
                blockExecutionReturnSet.Select(executionReturn =>
                    (executionReturn.TransactionId, executionReturn.Status)));

            var blockBody = new BlockBody();
            blockBody.Transactions.AddRange(allExecutedTransactionIds);

            var block = new Block
            {
                Header = blockHeader,
                Body = blockBody
            };

            blockBody.BlockHeader = blockHeader.GetHash();
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

        class ReturnSetContainer
        {
            private List<ExecutionReturnSet> _executed = new List<ExecutionReturnSet>();
            private List<Hash> _unexecutable = new List<Hash>();

            public List<ExecutionReturnSet> Executed => _executed;

            public List<Hash> Unexecutable => _unexecutable;

            public ReturnSetContainer(IEnumerable<ExecutionReturnSet> returnSets)
            {
                AddRange(returnSets);
            }

            public void AddRange(IEnumerable<ExecutionReturnSet> returnSets)
            {
                foreach (var returnSet in returnSets)
                {
                    if (returnSet.Status == TransactionResultStatus.Mined ||
                        returnSet.Status == TransactionResultStatus.Failed)
                    {
                        _executed.Add(returnSet);
                    }
                    else if (returnSet.Status == TransactionResultStatus.Unexecutable)
                    {
                        _unexecutable.Add(returnSet.TransactionId);
                    }
                }
            }

            public BlockStateSet ToBlockStateSet()
            {
                var blockStateSet = new BlockStateSet();
                foreach (var returnSet in _executed)
                {
                    foreach (var change in returnSet.StateChanges)
                    {
                        blockStateSet.Changes[change.Key] = change.Value;
                    }
                }

                return blockStateSet;
            }
        }
    }
}