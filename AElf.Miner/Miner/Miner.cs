using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.EventMessages;
using AElf.Common.Attributes;
using AElf.Common;
using AElf.Configuration;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Miner.Rpc.Exceptions;
using AElf.Miner.Rpc.Server;
using AElf.SmartContract;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Easy.MessageHub;
using NLog;
using NServiceKit.Common.Extensions;
using Status = AElf.Kernel.Status;
using AElf.Execution.Execution;
using AElf.Miner.EventMessages;
using AElf.Miner.Rpc.Client;
using AElf.Miner.TxMemPool;

// ReSharper disable once CheckNamespace
namespace AElf.Miner.Miner
{
    // ReSharper disable IdentifierTypo
    [LoggerName(nameof(Miner))]
    public class Miner : IMiner
    {
        private readonly ITxPool _txPool;
        private ECKeyPair _keyPair;
        private readonly IChainService _chainService;
        private readonly IExecutingService _executingService;
        private readonly ITransactionManager _transactionManager;
        private readonly ITransactionResultManager _transactionResultManager;
        private int _timeoutMilliseconds;
        private readonly ILogger _logger;
        private IBlockChain _blockChain;
        private readonly ClientManager _clientManager;
        private readonly IBinaryMerkleTreeManager _binaryMerkleTreeManager;
        private readonly ServerManager _serverManager;
        private Address _producerAddress;

        private IMinerConfig Config { get; }

        public Address Coinbase => Config.CoinBase;

        public Miner(IMinerConfig config, ITxPool txPool, IChainService chainService,
            IExecutingService executingService, ITransactionManager transactionManager,
            ITransactionResultManager transactionResultManager, ILogger logger, ClientManager clientManager, 
            IBinaryMerkleTreeManager binaryMerkleTreeManager, ServerManager serverManager)
        {
            Config = config;
            _txPool = txPool;
            _chainService = chainService;
            _executingService = executingService;
            _transactionManager = transactionManager;
            _transactionResultManager = transactionResultManager;
            _logger = logger;
            _clientManager = clientManager;
            _binaryMerkleTreeManager = binaryMerkleTreeManager;
            _serverManager = serverManager;
        }

        /// <inheritdoc />
        /// <summary>
        /// Mine process.
        /// </summary>
        /// <param name="currentRoundInfo"></param>
        /// <returns></returns>
        public async Task<IBlock> Mine(Round currentRoundInfo = null)
        {
            using (var cancellationTokenSource = new CancellationTokenSource())
            using (var timer = new Timer(s => cancellationTokenSource.Cancel()))
            {
                timer.Change(_timeoutMilliseconds, Timeout.Infinite);
                try
                {
                    if (cancellationTokenSource.IsCancellationRequested)
                        return null;

                    var parentChainBlockInfo = await GetParentChainBlockInfo();
                    var genTx = await GenerateTransactionWithParentChainBlockInfo(parentChainBlockInfo);
                    var readyTxs = await _txPool.GetReadyTxsAsync(currentRoundInfo);
                    // remove invalid CrossChainBlockInfoTransaction, only that from local can be executed)
                    /*readyTxs.RemoveAll(t =>
                        t.Type == TransactionType.CrossChainBlockInfoTransaction &&
                        !t.GetHash().Equals(genTx.GetHash()));*/
                    
                    var dposTxs = readyTxs.Where(tx => tx.Type == TransactionType.DposTransaction);
                    _logger?.Trace($"Will package {dposTxs.Count()} DPoS txs.");
                    foreach (var transaction in dposTxs)
                    {
                        _logger?.Trace($"{transaction.GetHash().DumpHex()} - {transaction.MethodName} from {transaction.From.DumpHex()}");
                    }

                    var disambiguationHash = HashHelpers.GetDisambiguationHash(await GetNewBlockIndexAsync(), _producerAddress);
                    _logger?.Log(LogLevel.Debug, "Executing Transactions..");
                    var traces = readyTxs.Count == 0
                        ? new List<TransactionTrace>()
                        : await _executingService.ExecuteAsync(readyTxs, Config.ChainId, cancellationTokenSource.Token, disambiguationHash);
                    _logger?.Log(LogLevel.Debug, "Executed Transactions.");

                    // transaction results
                    ExtractTransactionResults(readyTxs, traces, out var executed, out var rollback, out var results);

                    // generate block
                    var block = await GenerateBlockAsync(Config.ChainId, results);
                    _logger?.Log(LogLevel.Debug, $"Generated Block at height {block.Header.Index}");

                    // append block
                    await _blockChain.AddBlocksAsync(new List<IBlock> {block});
                    // put back canceled transactions
                    // No await so that it won't affect Consensus
                    _txPool.Revert(rollback);
                    // insert to db
                    Update(executed, results, block, parentChainBlockInfo, genTx);

                    MessageHub.Instance.Publish(new BlockMined(block));

                    return block;
                }
                catch (Exception e)
                {
                    _logger?.Error(e, "Mining failed with exception.");
                    return null;
                }
            }
        }

        private async Task<ulong> GetNewBlockIndexAsync()
        {
            var blockChain = _chainService.GetBlockChain(Config.ChainId);
            var index = await blockChain.GetCurrentBlockHeightAsync() + 1;
            return index;
        }

        private async Task UpdateParentChainBlockInfo(ParentChainBlockInfo parentChainBlockInfo)
        {
            await _clientManager.UpdateParentChainBlockInfo(parentChainBlockInfo);
        }

        /// <summary>
        /// Generate a system tx for parent chain block info and broadcast it.
        /// </summary>
        /// <param name="parentChainBlockInfo"></param>
        /// <returns></returns>
        private async Task<Transaction> GenerateTransactionWithParentChainBlockInfo(ParentChainBlockInfo parentChainBlockInfo)
        {
            if (parentChainBlockInfo == null)
                return null; 
            try
            {
                var bn = await _blockChain.GetCurrentBlockHeightAsync();
                var bh = bn == 0 ? Hash.Genesis : (await _blockChain.GetHeaderByHeightAsync(bn)).GetHash();
                var bhPref = bh.Value.Where((x, i) => i < 4).ToArray();
                var tx = new Transaction
                {
                    From = _keyPair.GetAddress(),
                    To = AddressHelpers.GetSystemContractAddress(Config.ChainId, SmartContractType.SideChainContract.ToString()),
                    RefBlockNumber = bn,
                    RefBlockPrefix = ByteString.CopyFrom(bhPref),
                    MethodName = "WriteParentChainBlockInfo",
                    P = ByteString.CopyFrom(_keyPair.GetEncodedPublicKey()),
                    Type = TransactionType.CrossChainBlockInfoTransaction,
                    Params = ByteString.CopyFrom(ParamsPacker.Pack(parentChainBlockInfo))
                };
                // sign tx
                var signature = new ECSigner().Sign(_keyPair, tx.GetHash().DumpByteArray());
                tx.R = ByteString.CopyFrom(signature.R);
                tx.S = ByteString.CopyFrom(signature.S);
                
                await BroadcastTransaction(tx);
                return tx;
            }
            catch (Exception e)
            {
                _logger?.Error(e, "PCB transaction generation failed.");
                return null;
            }
        }

        private async Task<bool> BroadcastTransaction(Transaction tx)
        {
            if (tx == null)
                return false;
            
            // insert to tx pool and broadcast
            var insertion = await _txPool.AddTxAsync(tx);
            if (insertion == TxValidation.TxInsertionAndBroadcastingError.Success)
            {
                MessageHub.Instance.Publish(new TransactionAddedToPool(tx));
                _logger.Debug($"Parent chain block info transaction insertion success. {tx.GetHash()}");
                return true;
            }
            _logger?.Debug($"Parent chain block info transaction insertion failed. {insertion}");
            return false;
        }
        
        /// <summary>
        /// Extract tx results from traces
        /// </summary>
        /// <param name="readyTxs"></param>
        /// <param name="traces"></param>
        /// <param name="executed"></param>
        /// <param name="rollback"></param>
        /// <param name="results"></param>
        private void ExtractTransactionResults(IEnumerable<Transaction> readyTxs, IEnumerable<TransactionTrace> traces,
            out List<Transaction> executed, out List<Transaction> rollback, out List<TransactionResult> results)
        {
            var canceledTxIds = new List<Hash>();
            results = new List<TransactionResult>();
            int index = 0;
            foreach (var trace in traces)
            {
                switch (trace.ExecutionStatus)
                {
                    case ExecutionStatus.Canceled:
                        // Put back transaction
                        canceledTxIds.Add(trace.TransactionId);
                        break;
                    case ExecutionStatus.ExecutedAndCommitted:
                        // Successful
                        var txRes = new TransactionResult()
                        {
                            TransactionId = trace.TransactionId,
                            Status = Status.Mined,
                            RetVal = ByteString.CopyFrom(trace.RetVal.ToFriendlyBytes()),
                            StateHash = trace.GetSummarizedStateHash(),
                            Index = index++,
                            Transaction = trace.Transaction
                        };
                        txRes.UpdateBloom();
                        results.Add(txRes);
                        break;
                    case ExecutionStatus.ContractError:
                        var txResF = new TransactionResult()
                        {
                            TransactionId = trace.TransactionId,
                            RetVal = ByteString.CopyFromUtf8(trace.StdErr), // Is this needed?
                            Status = Status.Failed,
                            StateHash = trace.GetSummarizedStateHash(),
                            Index = index++,
                            Transaction = trace.Transaction
                        };
                        results.Add(txResF);
                        break;
                    case ExecutionStatus.Undefined:
                        _logger?.Fatal(
                            $@"Transaction Id ""{
                                    trace.TransactionId
                                } is executed with status Undefined. Transaction trace: {trace}""");
                        break;
                    case ExecutionStatus.SystemError:
                        // SystemError shouldn't happen, and need to fix
                        _logger?.Fatal(
                            $@"Transaction Id ""{
                                    trace.TransactionId
                                } is executed with status SystemError. Transaction trace: {trace}""");
                        break;
                    case ExecutionStatus.ExecutedButNotCommitted:
                        // If this happens, there's problem with the code
                        _logger?.Fatal(
                            $@"Transaction Id ""{
                                    trace.TransactionId
                                } is executed with status ExecutedButNotCommitted. Transaction trace: {
                                    trace
                                }""");
                        break;
                }
            }

            var canceled = canceledTxIds.ToHashSet();
            executed = new List<Transaction>();
            rollback = new List<Transaction>();
            foreach (var tx in readyTxs)
            {
                if (canceled.Contains(tx.GetHash()))
                {
                    rollback.Add(tx);
                }
                else
                {
                    executed.Add(tx);
                }
            }
        }

        /// <summary>
        /// Update database
        /// </summary>
        /// <param name="executedTxs"></param>
        /// <param name="txResults"></param>
        /// <param name="block"></param>
        /// <param name="parentChainBlockInfo"></param>
        /// <param name="pcbTransaction"></param>
        private void Update(List<Transaction> executedTxs, List<TransactionResult> txResults, IBlock block, 
            ParentChainBlockInfo parentChainBlockInfo, Transaction pcbTransaction)
        {
            var bn = block.Header.Index;
            var bh = block.Header.GetHash();
            executedTxs.AsParallel().ForEach(async tx =>
                {
                    await _transactionManager.AddTransactionAsync(tx);
                    _txPool.RemoveAsync(tx.GetHash());
                });
            txResults.AsParallel().ForEach(async r =>
            {
                r.BlockNumber = bn;
                r.BlockHash = bh;
                r.MerklePath = block.Body.BinaryMerkleTree.GenerateMerklePath(r.Index);
                await _transactionResultManager.AddTransactionResultAsync(r);
                
                // update parent chain block info
                if (pcbTransaction != null && r.TransactionId.Equals(pcbTransaction.GetHash()) && r.Status.Equals(Status.Mined))
                {
                    await _clientManager.UpdateParentChainBlockInfo(parentChainBlockInfo);
                }
            });
            // update merkle tree
            _binaryMerkleTreeManager.AddTransactionsMerkleTreeAsync(block.Body.BinaryMerkleTree, Config.ChainId,
                block.Header.Index);
            _binaryMerkleTreeManager.AddSideChainTransactionRootsMerkleTreeAsync(
                block.Body.BinaryMerkleTreeForSideChainTransactionRoots, Config.ChainId, block.Header.Index);
        }

        /// <summary>
        /// Generate block
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="results"></param>
        /// <returns></returns>
        private async Task<IBlock> GenerateBlockAsync(Hash chainId, List<TransactionResult> results)
        {
            var blockChain = _chainService.GetBlockChain(chainId);

            var currentBlockHash = await blockChain.GetCurrentBlockHashAsync();
            var index = await blockChain.GetCurrentBlockHeightAsync() + 1;
            var block = new Block(currentBlockHash)
            {
                Header =
                {
                    Index = index,
                    ChainId = chainId,
                    Bloom = ByteString.CopyFrom(
                        Bloom.AndMultipleBloomBytes(
                            results.Where(x => !x.Bloom.IsEmpty).Select(x => x.Bloom.ToByteArray())
                        )
                    )
                }
            };

            // side chain info
            await CollectSideChainIndexedInfo(block);
            // add tx hash
            block.AddTransactions(results.Select(r => r.Transaction));

            // set ws merkle tree root
            block.Header.MerkleTreeRootOfWorldState = new BinaryMerkleTree().AddNodes(results.Select(x=>x.StateHash)).ComputeRootHash();
            block.Header.Time = Timestamp.FromDateTime(DateTime.UtcNow);

            // calculate and set tx merkle tree root 
            block.Complete();
            block.Sign(_keyPair);
            return block;
        }

        /// <summary>
        /// Generate block header
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="merkleTreeRootForTransaction"></param>
        /// <returns></returns>
        public async Task<IBlockHeader> GenerateBlockHeaderAsync(Hash chainId, Hash merkleTreeRootForTransaction)
        {
            // get ws merkle tree root
            var blockChain = _chainService.GetBlockChain(chainId);

            var lastBlockHash = await blockChain.GetCurrentBlockHashAsync();
            // TODO: Generic IBlockHeader
            var lastHeader = (BlockHeader) await blockChain.GetHeaderByHashAsync(lastBlockHash);
            var index = lastHeader.Index;
            var block = new Block(lastBlockHash) {Header = {Index = index + 1, ChainId = chainId}};

//            var ws = await _stateDictator.GetWorldStateAsync(lastBlockHash);
//            var state = await ws.GetWorldStateMerkleTreeRootAsync();

            var header = new BlockHeader
            {
                Version = 0,
                PreviousBlockHash = lastBlockHash,
                MerkleTreeRootOfWorldState = Hash.Default,
                MerkleTreeRootOfTransactions = merkleTreeRootForTransaction
            };

            return header;
        }

        /// <summary>
        /// Side chains header info    
        /// </summary>
        /// <returns></returns>
        private async Task CollectSideChainIndexedInfo(IBlock block)
        {
            // interval waiting for each side chain
            var sideChainInfo = await _clientManager.CollectSideChainBlockInfo();
            block.Body.IndexedInfo.Add(sideChainInfo);
        }

        /// <summary>
        /// Get parent chain block info.
        /// </summary>
        /// <returns></returns>
        private async Task<ParentChainBlockInfo> GetParentChainBlockInfo()
        {
            try
            {
                var blocInfo = await _clientManager.TryGetParentChainBlockInfo();
                return blocInfo;
            }
            catch (Exception e)
            {
                if (e is ClientShutDownException)
                    return null;
                throw;
            }
        }
        
        /// <summary>
        /// Start mining
        /// init clients to side chain node 
        /// </summary>
        public void Init()
        {
            _timeoutMilliseconds = GlobalConfig.AElfMiningInterval;
            _keyPair = NodeConfig.Instance.ECKeyPair;
            _producerAddress = Address.FromRawBytes(_keyPair.GetEncodedPublicKey());
            _blockChain = _chainService.GetBlockChain(Config.ChainId);
        }

        /// <summary>
        /// Stop mining
        /// </summary>
        public void Close()
        {
            _clientManager.CloseClientsToSideChain();
            _serverManager.Close();
        }
    }
}