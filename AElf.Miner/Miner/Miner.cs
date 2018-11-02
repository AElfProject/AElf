using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common.Attributes;
using AElf.Common;
using AElf.Configuration;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Miner.Rpc.Exceptions;
using AElf.Miner.Rpc.Server;
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
        private readonly ITxHub _txHub;
        private ECKeyPair _keyPair;
        private readonly IChainService _chainService;
        private readonly IExecutingService _executingService;
        private readonly ITransactionResultManager _transactionResultManager;
        private int _timeoutMilliseconds;
        private readonly ILogger _logger;
        private IBlockChain _blockChain;
        private readonly ClientManager _clientManager;
        private readonly IBinaryMerkleTreeManager _binaryMerkleTreeManager;
        private readonly ServerManager _serverManager;
        private readonly IBlockValidationService _blockValidationService;
        private readonly IChainContextService _chainContextService;
        private Address _producerAddress;

        private IMinerConfig Config { get; }

        public Address Coinbase => Config.CoinBase;
        private readonly TransactionFilter _txFilter;

        public Miner(IMinerConfig config, ITxHub txHub, IChainService chainService,
            IExecutingService executingService, ITransactionResultManager transactionResultManager,
            ILogger logger, ClientManager clientManager,
            IBinaryMerkleTreeManager binaryMerkleTreeManager, ServerManager serverManager,
            IBlockValidationService blockValidationService, IChainContextService chainContextService)
        {
            Config = config;
            _txHub = txHub;
            _chainService = chainService;
            _executingService = executingService;
            _transactionResultManager = transactionResultManager;
            _logger = logger;
            _clientManager = clientManager;
            _binaryMerkleTreeManager = binaryMerkleTreeManager;
            _serverManager = serverManager;
            _blockValidationService = blockValidationService;
            _chainContextService = chainContextService;
            _txFilter = new TransactionFilter();
        }

        /// <inheritdoc />
        /// <summary>
        /// Mine process.
        /// </summary>
        /// <param name="currentRoundInfo"></param>
        /// <returns></returns>
        public async Task<IBlock> Mine(Round currentRoundInfo = null)
        {
            try
            {
                var parentChainBlockInfo = await GetParentChainBlockInfo();
                var genTx = await GenerateTransactionWithParentChainBlockInfo(parentChainBlockInfo);
                var txs = await _txHub.GetReceiptsOfExecutablesAsync();
                var txGrp = txs.GroupBy(tr => tr.IsSystemTxn).ToDictionary(x => x.Key, x => x.ToList());
                var readyTxs = new List<Transaction>();
                var traces = new List<TransactionTrace>();
                if (txGrp.TryGetValue(true, out var sysRcpts))
                {
                    var sysTxs = sysRcpts.Select(x => x.Transaction).ToList();
                    var needFilter = currentRoundInfo != null;
                    if (needFilter)
                    {
                        sysTxs = FilterDpos(sysTxs);
                    }

                    readyTxs = sysTxs;
                    _logger?.Trace($"Start executing {sysTxs.Count} system transactions.");
                    traces = await ExecuteTransactions(sysTxs, noTimeout: true);
                    _logger?.Trace($"Finish executing {sysTxs.Count} system transactions.");
                }

                if (txGrp.TryGetValue(false, out var regRcpts))
                {
                    var regTxs = regRcpts.Select(x => x.Transaction).ToList();
                    readyTxs.AddRange(regTxs);
                    _logger?.Trace($"Start executing {regTxs.Count} regular transactions.");
                    traces.AddRange(await ExecuteTransactions(regTxs));
                    _logger?.Trace($"Finish executing {regTxs.Count} regular transactions.");
                }

                ExtractTransactionResults(readyTxs, traces, out var executed, out var rollback, out var results);

                // generate block
                var block = await GenerateBlockAsync(Config.ChainId, results);
                _logger?.Info(
                    $"Generate block {block.BlockHashToHex} at height {block.Header.Index} with {block.Body.TransactionsCount} txs.");

                // We need at least check the txs count of this block.
                var chainContext =
                    await _chainContextService.GetChainContextAsync(Hash.LoadHex(NodeConfig.Instance.ChainId));
                var blockValidationResult = await _blockValidationService.ValidatingOwnBlock(true)
                    .ValidateBlockAsync(block, chainContext);
                if (blockValidationResult != BlockValidationResult.Success)
                {
                    _logger?.Warn($"Found the block generated before invalid: {blockValidationResult}.");
                    return null;
                }

                // append block
                await _blockChain.AddBlocksAsync(new List<IBlock> {block});

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

        private List<Transaction> FilterDpos(List<Transaction> txs)
        {
            var txGroup = txs.GroupBy(tx => tx.Type == TransactionType.DposTransaction)
                .ToDictionary(x => x.Key, x => x.ToList());

            if (txGroup.TryGetValue(true, out var dposTxs))
            {
                _txFilter.Execute(dposTxs);
            }

            return txs;
        }

        private async Task<List<TransactionTrace>> ExecuteTransactions(List<Transaction> txs, bool noTimeout=false)
        {
            using (var cts = new CancellationTokenSource())
            using (var timer = new Timer(s => cts.Cancel()))
            {
                timer.Change(_timeoutMilliseconds, Timeout.Infinite);

                if (cts.IsCancellationRequested)
                    return null;
                var disambiguationHash =
                    HashHelpers.GetDisambiguationHash(await GetNewBlockIndexAsync(), _producerAddress);

                var traces = txs.Count == 0
                    ? new List<TransactionTrace>()
                    : await _executingService.ExecuteAsync(txs, Config.ChainId,
                        noTimeout ? CancellationToken.None : cts.Token,
                        disambiguationHash);

                return traces;
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
        private async Task<Transaction> GenerateTransactionWithParentChainBlockInfo(
            ParentChainBlockInfo parentChainBlockInfo)
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
                    To = AddressHelpers.GetSystemContractAddress(Config.ChainId,
                        SmartContractType.SideChainContract.ToString()),
                    RefBlockNumber = bn,
                    RefBlockPrefix = ByteString.CopyFrom(bhPref),
                    MethodName = "WriteParentChainBlockInfo",
                    Sig = new Signature
                    {
                        P = ByteString.CopyFrom(_keyPair.GetEncodedPublicKey())
                    },
                    Type = TransactionType.CrossChainBlockInfoTransaction,
                    Params = ByteString.CopyFrom(ParamsPacker.Pack(parentChainBlockInfo))
                };
                // sign tx
                var signature = new ECSigner().Sign(_keyPair, tx.GetHash().DumpByteArray());
                tx.Sig.R = ByteString.CopyFrom(signature.R);
                tx.Sig.S = ByteString.CopyFrom(signature.S);

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
            await _txHub.AddTransactionAsync(tx);

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
                            Index = index++
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
                            Index = index++
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
            txResults.AsParallel().ForEach(async r =>
            {
                r.BlockNumber = bn;
                r.BlockHash = bh;
                r.MerklePath = block.Body.BinaryMerkleTree.GenerateMerklePath(r.Index);
                await _transactionResultManager.AddTransactionResultAsync(r);

                // update parent chain block info
                if (pcbTransaction != null && r.TransactionId.Equals(pcbTransaction.GetHash()) &&
                    r.Status.Equals(Status.Mined))
                {
                    await _clientManager.UpdateParentChainBlockInfo(parentChainBlockInfo);
                }
            });
            // update merkle tree
            _binaryMerkleTreeManager.AddTransactionsMerkleTreeAsync(block.Body.BinaryMerkleTree, Config.ChainId,
                block.Header.Index);
            if (block.Body.IndexedInfo.Count > 0)
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
            block.AddTransactions(results.Select(x => x.TransactionId));

            // set ws merkle tree root
            block.Header.MerkleTreeRootOfWorldState =
                new BinaryMerkleTree().AddNodes(results.Select(x => x.StateHash)).ComputeRootHash();
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