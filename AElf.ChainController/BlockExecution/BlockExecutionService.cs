using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController.TxMemPool;
using AElf.Common;
using AElf.Configuration;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.SmartContract;
using Akka.Util.Internal;
using Google.Protobuf;
using NLog;

// ReSharper disable once CheckNamespace
namespace AElf.ChainController
{
    // ReSharper disable InconsistentNaming
    public class BlockExecutionService : IBlockExecutionService
    {
        private readonly IExecutingService _executingService;
        private readonly ITxPoolService _txPoolService;
        private readonly IChainService _chainService;
        private readonly ITransactionManager _transactionManager;
        private readonly ITransactionResultManager _transactionResultManager;
        private readonly IBinaryMerkleTreeManager _binaryMerkleTreeManager;

        private readonly ILogger _logger;

        public BlockExecutionService(IExecutingService executingService, ITxPoolService txPoolService,
            IChainService chainService, ITransactionManager transactionManager,
            ITransactionResultManager transactionResultManager,
            IBinaryMerkleTreeManager binaryMerkleTreeManager)
        {
            _executingService = executingService;
            _txPoolService = txPoolService;
            _chainService = chainService;
            _transactionManager = transactionManager;
            _transactionResultManager = transactionResultManager;
            _binaryMerkleTreeManager = binaryMerkleTreeManager;

            _logger = LogManager.GetLogger(nameof(BlockExecutionService));

            _logger?.Trace("Using new block execution service.");
        }

        /// <summary>
        /// Signals to a CancellationToken that mining should be canceled
        /// </summary>
        private CancellationTokenSource Cts { get; set; }

        public async Task<BlockExecutionResultCC> ExecuteBlock(IBlock block)
        {
            if (!await Prepare(block))
            {
                return BlockExecutionResultCC.Failed;
            }

            _logger?.Trace($"Executing block {block.GetHash()}");

            var txs = new List<Transaction>();
            try
            {
                txs = block.Body.TransactionList.ToList();
                var disambiguationHash = HashHelpers.GetDisambiguationHash(block.Header.Index,
                    Address.FromRawBytes(block.Header.P.ToByteArray()));
                var txResults = await ExecuteTransactions(txs, Hash.LoadHex(NodeConfig.Instance.ChainId), disambiguationHash);
                await InsertTxs(txs, txResults, block);
                var res = await UpdateState(block, txResults);
                var blockchain = _chainService.GetBlockChain(block.Header.ChainId);

                if (!res)
                {
                    var txToRevert = await blockchain.RollbackOneBlock();
                    await _txPoolService.Revert(txToRevert);
                    return BlockExecutionResultCC.Failed;
                }

                await blockchain.AddBlocksAsync(new List<IBlock> {block});
                await _binaryMerkleTreeManager.AddTransactionsMerkleTreeAsync(block.Body.BinaryMerkleTree,
                    block.Header.ChainId,
                    block.Header.Index);
                await _binaryMerkleTreeManager.AddSideChainTransactionRootsMerkleTreeAsync(
                    block.Body.BinaryMerkleTreeForSideChainTransactionRoots, block.Header.ChainId, block.Header.Index);
            }
            catch (Exception e)
            {
                await Interrupt($"ExecuteBlock - Execution failed with exception {e}", txs, e);
                return BlockExecutionResultCC.Failed;
            }

            return BlockExecutionResultCC.Success;
        }

        /// <summary>
        /// Verify block components and validate side chain info if needed
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        private async Task<bool> Prepare(IBlock block)
        {
            string errLog = null;
            var res = true;
            if (Cts == null || Cts.IsCancellationRequested)
            {
                errLog = "ExecuteBlock - Execution cancelled.";
                res = false;
            }
            else if (block?.Header == null || block.Body?.Transactions == null || block.Body.Transactions.Count <= 0)
            {
                _logger?.Trace("Latest Block txs count:");
                _logger?.Trace(block?.Body?.TransactionsCount);
                errLog = "ExecuteBlock - Null block or no transactions.";
                res = false;
            }

            if (!res)
                await Interrupt(errLog);
            return res;
        }

        private async Task<List<TransactionResult>> ExecuteTransactions(List<Transaction> txs, Hash chainId, Hash disambiguationHash)
        {
            try
            {
                var traces = txs.Count == 0
                    ? new List<TransactionTrace>()
                    : await _executingService.ExecuteAsync(txs, chainId, Cts.Token, disambiguationHash);

                var results = new List<TransactionResult>();
                foreach (var trace in traces)
                {
                    var res = new TransactionResult
                    {
                        TransactionId = trace.TransactionId,
                    };
                    if (string.IsNullOrEmpty(trace.StdErr))
                    {
                        res.Logs.AddRange(trace.FlattenedLogs);
                        res.Status = Status.Mined;
                        res.RetVal = ByteString.CopyFrom(trace.RetVal.ToFriendlyBytes());
                        res.StateHash = trace.GetSummarizedStateHash();
                    }
                    else
                    {
                        res.Status = Status.Failed;
                        res.RetVal = ByteString.CopyFromUtf8(trace.StdErr);
                    }

                    results.Add(res);
                }

                return results;
            }
            catch (Exception e)
            {
                await Interrupt(e.ToString(), txs, e);
                return null;
            }
        }

        private async Task Interrupt(string log, List<Transaction> txs = null, Exception e = null)
        {
            if (e == null)
                _logger.Debug(log);
            else
                _logger.Error(e, log);
            await Rollback(txs);
        }

        /// <summary>
        /// Withdraw txs in tx pool
        /// </summary>
        /// <param name="readyTxs"></param>
        /// <returns></returns>
        private async Task Rollback(List<Transaction> readyTxs)
        {
//            await _stateDictator.RollbackToPreviousBlock();
            if (readyTxs == null)
                return;
            await _txPoolService.Revert(readyTxs);
        }

        /// <summary>
        /// Update database
        /// </summary>
        /// <param name="executedTxs"></param>
        /// <param name="txResults"></param>
        /// <param name="block"></param>
        private async Task<HashSet<Address>> InsertTxs(IEnumerable<Transaction> executedTxs,
            IEnumerable<TransactionResult> txResults, IBlock block)
        {
            var bn = block.Header.Index;
            var bh = block.Header.GetHash();
            var address = new HashSet<Address>();
            foreach (var t in executedTxs)
            {
                address.Add(t.From);
                await _transactionManager.AddTransactionAsync(t);
            }

            txResults.AsParallel().ForEach(async r =>
            {
                r.BlockNumber = bn;
                r.BlockHash = bh;
                await _transactionResultManager.AddTransactionResultAsync(r);
            });
            return address;
        }

        /// <summary>
        /// Update system state.
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        private async Task<bool> UpdateState(IBlock block, IEnumerable<TransactionResult> results)
        {
            var root = new BinaryMerkleTree().AddNodes(results.Select(x => x.StateHash)).ComputeRootHash();
            string errlog = null;
            bool res = true;
            if (root != block.Header.MerkleTreeRootOfWorldState)
            {
                errlog = "ExecuteBlock - Incorrect merkle trees.";
                res = false;
            }
            if(!res)
                await Interrupt(errlog);
            return res;
        }

        public void Init()
        {
            Cts = new CancellationTokenSource();
        }
    }
}