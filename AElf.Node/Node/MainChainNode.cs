using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Common.ByteArrayHelpers;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Managers;
using AElf.Kernel.Node.Config;
using AElf.Kernel.Node.Protocol;
using AElf.Kernel.Node.RPC;
using AElf.Kernel.Node.RPC.DTO;
using AElf.ChainController;
using AElf.Kernel.Consensus;
using AElf.SmartContract;
using AElf.Execution;
using AElf.Execution.Scheduling;
using AElf.Network.Data;
using AElf.Types.CSharp;
using Akka.Actor;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using ServiceStack;

namespace AElf.Kernel.Node
{
    [LoggerName("Node")]
    public class MainChainNode : IAElfNode
    {
        private ECKeyPair _nodeKeyPair;
        private ActorSystem _sys = ActorSystem.Create("AElf");
        private readonly IBlockManager _blockManager;
        private readonly ITxPoolService _txPoolService;
        private readonly ITransactionManager _transactionManager;
        private readonly IRpcServer _rpcServer;
        private readonly ILogger _logger;
        private readonly IProtocolDirector _protocolDirector;
        private readonly INodeConfig _nodeConfig;
        private readonly IMiner _miner;
        private readonly IAccountContextService _accountContextService;
        private readonly IBlockVaildationService _blockVaildationService;
        private readonly IChainContextService _chainContextService;
        private readonly IChainManager _chainManager;
        private readonly IChainCreationService _chainCreationService;
        private readonly IWorldStateDictator _worldStateDictator;
        private readonly ISmartContractService _smartContractService;
        private readonly ITransactionResultService _transactionResultService;
        private readonly IFunctionMetadataService _functionMetadataService;

        private readonly IBlockExecutor _blockExecutor;

        private readonly AElfDPoSHelper _dPoSHelper;

        public Hash ContractAccountHash => _chainCreationService.GenesisContractHash(_nodeConfig.ChainId);

        public IObservable<ConsensusBehavior> ConsensusObservable;

        private const int CheckTime = 1000;

        private int _flag;
        public bool IsMining { get; private set; }

        private readonly Stack<Hash> _consensusData = new Stack<Hash>();

        public int IsMiningInProcess => _flag;

        public BlockProducer BlockProducers
        {
            get
            {
                var dict = MinersInfo.Instance.Producers;
                var blockProducers = new BlockProducer();
                foreach (var bp in dict.Values)
                {
                    var b = bp["address"].RemoveHexPrefix();
                    blockProducers.Nodes.Add(b);
                }

                Globals.BlockProducerNumber = blockProducers.Nodes.Count;

                return blockProducers;
            }
        }

        public IObserver<ConsensusBehavior> ConsensusSequence => new AElfDPoSObservable(_logger, MiningWithInitializingAElfDPoSInformation,
            MiningWithPublishingOutValueAndSignature, PublishInValue, MiningWithUpdatingAElfDPoSInformation);

        public Hash ChainId => _nodeConfig.ChainId;

        public MainChainNode(ITxPoolService poolService, ITransactionManager txManager, IRpcServer rpcServer,
            IProtocolDirector protocolDirector, ILogger logger, INodeConfig nodeConfig, IMiner miner,
            IAccountContextService accountContextService, IBlockVaildationService blockVaildationService,
            IChainContextService chainContextService, IBlockExecutor blockExecutor,
            IChainCreationService chainCreationService, IWorldStateDictator worldStateDictator, 
            IChainManager chainManager, ISmartContractService smartContractService,
            ITransactionResultService transactionResultService, IBlockManager blockManager, 
            IFunctionMetadataService functionMetadataService)
        {
            _chainCreationService = chainCreationService;
            _chainManager = chainManager;
            _worldStateDictator = worldStateDictator;
            _smartContractService = smartContractService;
            _transactionResultService = transactionResultService;
            _blockManager = blockManager;
            _functionMetadataService = functionMetadataService;
            _txPoolService = poolService;
            _protocolDirector = protocolDirector;
            _transactionManager = txManager;
            _rpcServer = rpcServer;
            _logger = logger;
            _nodeConfig = nodeConfig;
            _miner = miner;
            _accountContextService = accountContextService;
            _blockVaildationService = blockVaildationService;
            _chainContextService = chainContextService;
            _worldStateDictator = worldStateDictator;
            _blockExecutor = blockExecutor;
            
            _dPoSHelper = new AElfDPoSHelper(_worldStateDictator, _nodeKeyPair, ChainId, BlockProducers, ContractAccountHash, _logger);
        }

        public bool Start(ECKeyPair nodeKeyPair, bool startRpc, int rpcPort, string rpcHost, string initData, byte[] code)
        {
            if (_nodeConfig == null)
            {
                _logger?.Log(LogLevel.Error, "No node configuration.");
                return false;
            }
            
            if (_nodeConfig.ChainId?.Value == null || _nodeConfig.ChainId.Value.Length <= 0)
            {
                _logger?.Log(LogLevel.Error, "No chain id.");
                return false;
            }

            try
            {
                bool chainExists = _chainManager.Exists(_nodeConfig.ChainId).Result;
            
                if (!chainExists)
                {
                    // Creation of the chain if it doesn't already exist
                    var smartContractZeroReg = new SmartContractRegistration
                    {
                        Category = 0, 
                        ContractBytes = ByteString.CopyFrom(code),
                        ContractHash = code.CalculateHash()
                    };
                    var res = _chainCreationService.CreateNewChainAsync(_nodeConfig.ChainId, smartContractZeroReg)
                        .Result;
                    
                    _logger?.Log(LogLevel.Debug, "Chain Id = \"{0}\"", _nodeConfig.ChainId.ToHex());
                    _logger?.Log(LogLevel.Debug, "Genesis block hash = \"{0}\"", res.GenesisBlockHash.ToHex());
                    var contractAddress = GetGenesisContractHash();
                    _logger?.Log(LogLevel.Debug, "HEX Genesis contract address = \"{0}\"",
                        contractAddress.ToAccount().ToHex());
                    
                }
                else
                {
                    var preBlockHash = GetLastValidBlockHash().Result;
                    _worldStateDictator.SetWorldStateAsync(preBlockHash);
                    
                    _worldStateDictator.PreBlockHash = preBlockHash;
                    _worldStateDictator.RollbackCurrentChangesAsync();
                }
            }
            catch (Exception e)
            {
                _logger?.Log(LogLevel.Error,
                    "Could not create the chain : " + _nodeConfig.ChainId.ToHex());
            }
            
            
            if (!string.IsNullOrWhiteSpace(initData))
            {
                /*if (!InitialDebugSync(initdata).Result)
                {
                    //todo log 
                    return false;
                }*/
            }
            
            // set world state
            _worldStateDictator.SetChainId(_nodeConfig.ChainId);
            
            _nodeKeyPair = nodeKeyPair;
            
            if (startRpc)
                _rpcServer.Start(rpcHost, rpcPort);

            _txPoolService.Start();
            _protocolDirector.Start();

            // todo : avoid circular dependency
            _rpcServer.SetCommandContext(this);
            _protocolDirector.SetCommandContext(this, true); // If not miner do sync
            
            // akka env 
            

            var servicePack = new ServicePack
            {
                ChainContextService = _chainContextService,
                SmartContractService = _smartContractService,
                ResourceDetectionService = new ResourceUsageDetectionService(_functionMetadataService),
                WorldStateDictator = _worldStateDictator
            };
            
            var grouper = new Grouper(servicePack.ResourceDetectionService, _logger);
            _blockExecutor.Start(grouper);
            
            if (_nodeConfig.IsMiner)
            {
                _miner.Start(nodeKeyPair, grouper);
                
                //DoDPos();
                _logger?.Log(LogLevel.Debug, "Coinbase = \"{0}\"", _miner.Coinbase.ToHex());
            }
            
            _logger?.Log(LogLevel.Debug, "AElf node started.");
            
            return true;
        }


        public bool IsMiner()
        {
            return _nodeConfig.IsMiner;
        }
        
        private async Task<bool> InitialDebugSync(string initFileName)
        {
            try
            {
                string appFolder = _nodeConfig.DataDir;
                var fullPath = Path.Combine(appFolder, "tests", initFileName);

                /*Block b = null;
                using (StreamReader r = new StreamReader(fullPath))
                {
                    string jsonChain = r.ReadToEnd();
                    b = JsonParser.Default.Parse<Block>(jsonChain);
                }*/
                
                using (StreamReader file = File.OpenText(fullPath))
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    JObject balances = (JObject)JToken.ReadFrom(reader);
                    
                    foreach (var kv in balances)
                    {
                        var address = ByteArrayHelpers.FromHexString(kv.Key);
                        var balance = kv.Value.ToObject<ulong>();
                        
                        var accountDataProvider = await _worldStateDictator.GetAccountDataProvider(address);
                        var dataProvider = accountDataProvider.GetDataProvider();
                        
                        // set balance
                        await dataProvider.SetAsync("Balance".CalculateHash(),
                            new UInt64Value {Value = balance}.ToByteArray());
                        var str = $"Initial balance {balance} in Address \"{kv.Key}\""; 
                        _logger?.Log(LogLevel.Debug, "Initial balance {0} in Address \"{1}\"", balance, kv.Key);
                    }
                }
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// get the tx from tx pool or database
        /// </summary>
        /// <param name="txId"></param>
        /// <returns></returns>
        public async Task<ITransaction> GetTransaction(Hash txId)
        {
            if (_txPoolService.TryGetTx(txId, out var tx))
            {
                return tx;
            }

            return await _transactionManager.GetTransaction(txId);
        }

        /// <summary>
        /// This inserts a transaction into the node. Note that it does
        /// not broadcast it to the network and doesn't add it to the
        /// transaction pool. Essentially it just inserts the transaction
        /// in the database.
        /// </summary>
        /// <param name="tx">The transaction to insert</param>
        /// <returns>The hash of the transaction that was inserted</returns>
        public async Task<IHash> InsertTransaction(Transaction tx)
        {
            return await _transactionManager.AddTransactionAsync(tx);
        }

        /// <summary>
        /// This method processes a transaction received from one of the
        /// connected peers.
        /// </summary>
        /// <param name="messagePayload"></param>
        /// <param name="isFromSend"></param>
        /// <returns></returns>
        public async Task ReceiveTransaction(ByteString messagePayload, bool isFromSend)
        {
            try
            {
                var tx = Transaction.Parser.ParseFrom(messagePayload);
                
                var success = await _txPoolService.AddTxAsync(tx);
                
                if (isFromSend)
                {
                    _logger?.Trace("Received Transaction: " + "FROM, " + tx.GetHash().ToHex() + ", INCR : " + tx.IncrementId);
                    _protocolDirector.AddTransaction(tx);
                }

                if (success != TxValidation.TxInsertionAndBroadcastingError.Success)
                {
                    _logger?.Trace("DID NOT add Transaction to pool: FROM {0} , INCR : {1}, with error {2} ", tx.GetTransactionInfo() ,
                        tx.IncrementId, success);
                    return;
                }
                
                _logger?.Trace("Successfully added tx : " + tx.GetHash().Value.ToByteArray().ToHex());
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Invalid tx - Could not receive transaction from the network", null);
            }
        }

        /// <summary>
        /// This method requests a specified number of peers from
        /// the node's peer list.
        /// </summary>
        /// <param name="numPeers"></param>
        /// <returns></returns>
        public async Task<List<NodeData>> GetPeers(ushort? numPeers)
        {
            return _protocolDirector.GetPeers(numPeers);
        }

        /// <summary>
        /// return default incrementId for one address
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public async Task<ulong> GetIncrementId(Hash addr)
        {
            try
            {
                // ReSharper disable once InconsistentNaming
                var idInDB = (await _accountContextService.GetAccountDataContext(addr, _nodeConfig.ChainId))
                    .IncrementId;
                var idInPool = await _txPoolService.GetIncrementId(addr);

                return Math.Max(idInDB, idInPool);
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        public async Task<Hash> GetLastValidBlockHash()
        {
            var pointer = ResourcePath.CalculatePointerForLastBlockHash(_nodeConfig.ChainId);
            return await _worldStateDictator.GetDataAsync(pointer);
        }

        /// <summary>
        /// Add a new block received from network by first validating it and then
        /// executing it.
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public async Task<BlockExecutionResult> ExecuteAndAddBlock(IBlock block)
        {
            try
            {
                int res = Interlocked.CompareExchange(ref _flag, 1, 0);
                
                if (res == 1)
                    return new BlockExecutionResult(false, ValidationError.Mining);
                
                var context = await _chainContextService.GetChainContextAsync(_nodeConfig.ChainId);
                var error = await _blockVaildationService.ValidateBlockAsync(block, context, _nodeKeyPair);
                
                if (error != ValidationError.Success)
                {
                    var localCorrespondingBlock = await _blockManager.GetBlockByHeight(_nodeConfig.ChainId, block.Header.Index);
                    if (error == ValidationError.OrphanBlock) 
                    {
                        //TODO: limit the count of blocks to rollback
                        if (block.Header.Time.ToDateTime() < localCorrespondingBlock.Header.Time.ToDateTime())
                        {
                            _logger?.Trace("Ready to rollback");
                            //Rollback world state
                            var txs = await _worldStateDictator.RollbackToSpecificHeight(block.Header.Index);
                        
                            await _txPoolService.RollBack(txs);
                            _worldStateDictator.PreBlockHash = block.Header.PreviousBlockHash;
                            await _worldStateDictator.RollbackCurrentChangesAsync();

                            var ws = await _worldStateDictator.GetWorldStateAsync(block.GetHash());
                            _logger?.Trace($"Current world state {(await ws.GetWorldStateMerkleTreeRootAsync()).ToHex()}");

                            error = ValidationError.Success;
                        }
                        else
                        {
                            // insert to database 
                            Interlocked.CompareExchange(ref _flag, 0, 1);
                            return new BlockExecutionResult(false, ValidationError.OrphanBlock);
                        }
                    }
                    else
                    {
                        Interlocked.CompareExchange(ref _flag, 0, 1);
                        _logger?.Trace("Invalid block received from network: " + error);
                        return new BlockExecutionResult(false, error);
                    }
                }

                var executed = await _blockExecutor.ExecuteBlock(block);
                Interlocked.CompareExchange(ref _flag, 0, 1);

                return new BlockExecutionResult(executed, error);
                //return new BlockExecutionResult(true, error);
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Block synchronzing failed");
                Interlocked.CompareExchange(ref _flag, 0, 1);
                return new BlockExecutionResult(e);
            }
        }

        /// <summary>
        /// get missing tx hashes for the block. If an exception occured it return
        /// null. If there's simply no transaction from this block in the pool it
        /// returns an empty list.
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public List<Hash> GetMissingTransactions(IBlock block)
        {
            try
            {
                var res = new List<Hash>();
                var txs = block.Body.Transactions;
                foreach (var id in txs)
                {
                    if (!_txPoolService.TryGetTx(id, out var _))
                    {
                        res.Add(id);
                    }
                }

                return res;
            }
            catch (Exception e)
            {
                _logger?.Trace("Error while getting missing transactions");
                return null;
            }
        }

        public async Task<ulong> GetCurrentChainHeight()
        {
            IChainContext chainContext = await _chainContextService.GetChainContextAsync(_nodeConfig.ChainId);
            return chainContext.BlockHeight;
        }

        /// <summary>
        /// add tx
        /// </summary>
        /// <param name="tx"></param>
        /// <returns></returns>
        public async Task<TxValidation.TxInsertionAndBroadcastingError> AddTransaction(ITransaction tx)
        {
            return await _txPoolService.AddTxAsync(tx);
        }
        
        private static int currentIncr = 0;
        
        private Transaction GetFakeTx()
        {
            ECKeyPair keyPair = new KeyPairGenerator().Generate();
            ECSigner signer = new ECSigner();
            var txDep = new Transaction
            {
                From = keyPair.GetAddress(),
                To = GetGenesisContractHash(),
                IncrementId = (ulong)currentIncr++,
            };
            
            Hash hash = txDep.GetHash();

            ECSignature signature = signer.Sign(keyPair, hash.GetHashBytes());
            txDep.P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            txDep.R = ByteString.CopyFrom(signature.R); 
            txDep.S = ByteString.CopyFrom(signature.S);

            return txDep;
        }

        private ITransaction InvokTxDemo(ECKeyPair keyPair, Hash hash, string methodName, byte[] param, ulong index)
        {
            ECSigner signer = new ECSigner();
            var txInv = new Transaction
            {
                From = keyPair.GetAddress(),
                To = hash,
                IncrementId = index,
                MethodName = methodName,
                Params = ByteString.CopyFrom(param),
                
                Fee = TxPoolConfig.Default.FeeThreshold + 1
            };
            
            Hash txhash = txInv.GetHash();

            ECSignature signature = signer.Sign(keyPair, txhash.GetHashBytes());
            txInv.P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            txInv.R = ByteString.CopyFrom(signature.R); 
            txInv.S = ByteString.CopyFrom(signature.S);

            var res = BroadcastTransaction(txInv).Result;
            return txInv;
        }
        
        
        
        private ITransaction DeployTxDemo(ECKeyPair keyPair)
        {
            var ContractName = "AElf.Kernel.Tests.TestContract";
            var contractZeroDllPath = $"../{ContractName}/bin/Debug/netstandard2.0/{ContractName}.dll";
            
            byte[] code = null;
            using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath(contractZeroDllPath)))
            {
                code = file.ReadFully();
            }
            
            ECSigner signer = new ECSigner();
            var txDep = new Transaction
            {
                From = keyPair.GetAddress(),
                To = GetGenesisContractHash(),
                IncrementId = 0,
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(ParamsPacker.Pack(0, code)),
                
                Fee = TxPoolConfig.Default.FeeThreshold + 1
            };
            
            Hash hash = txDep.GetHash();

            ECSignature signature = signer.Sign(keyPair, hash.GetHashBytes());
            txDep.P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded());
            txDep.R = ByteString.CopyFrom(signature.R); 
            txDep.S = ByteString.CopyFrom(signature.S);
            var res = BroadcastTransaction(txDep).Result;

            return txDep;
        }
        
        
        public Hash GetGenesisContractHash()
        {
            return _chainCreationService.GenesisContractHash(_nodeConfig.ChainId);
        }
        
        
        /// <summary>
        /// temple mine to generate fake block data with loop
        /// </summary>
        public async Task StartConsensusProcess()
        {
            if (IsMining)
                return;

            IsMining = true;

            if (_dPoSHelper.CurrentRoundNumber.Value == 0)
            {
                AElfDPoSObservable.Initialization(ConsensusSequence);
            }
            else
            {
                var infoOfMe =
                    await _dPoSHelper.GetBPInfoOfCurrentRound(_nodeKeyPair.GetAddress().ToHex().RemoveHexPrefix());
                var extraBlockTimeslot = await _dPoSHelper.GetExtraBlockTimeslotOfCurrentRound();
                AElfDPoSObservable.NormalMiningProcess(infoOfMe, extraBlockTimeslot, ConsensusSequence);
            }
        }

        public async Task<IBlock> Mine()
        {
            int res = Interlocked.CompareExchange(ref _flag, 1, 0);

            if (res == 1)
                return null;
            try
            {
                _logger?.Trace($"Mine - Entered mining {res}");
            
                _worldStateDictator.BlockProducerAccountAddress = _nodeKeyPair.GetAddress();

                var block =  await _miner.Mine();
            
                int b = Interlocked.CompareExchange(ref _flag, 0, 1);

                _protocolDirector.IncrementChainHeight();
            
                _logger?.Trace($"Mine - Leaving mining {b}");
            
                return block;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Interlocked.CompareExchange(ref _flag, 0, 1);
                return null;
            }
        }

        public async Task<bool> BroadcastBlock(IBlock block)
        {
            if (block == null)
            {
                return false;
            }

            var count = await _protocolDirector.BroadcastBlock(block as Block);

            var bh = block.GetHash().ToHex();
            _logger?.Trace($"Broadcasted block \"{bh}\"  to [" +
                          count + $"] peers. Block height: [{block.Header.Index}]");

            return true;
        }

        public async Task<IMessage> GetContractAbi(Hash address)
        {
            return await _smartContractService.GetAbiAsync(address);
        }

        /// <summary>
        /// Broadcasts a transaction to the network. This method
        /// also places it in the transaction pool.
        /// </summary>
        /// <param name="tx">The tx to broadcast</param>
        public async Task<TxValidation.TxInsertionAndBroadcastingError> BroadcastTransaction(ITransaction tx)
        {
            TxValidation.TxInsertionAndBroadcastingError res;

            try
            {
                res = await _txPoolService.AddTxAsync(tx);
            }
            catch (Exception e)
            {
                _logger?.Trace("Transaction insertion failed: {0},\n{1}", e.Message, tx.GetTransactionInfo());
                return TxValidation.TxInsertionAndBroadcastingError.Failed;
            }

            if (res == TxValidation.TxInsertionAndBroadcastingError.Success)
            {
                try
                {
                    await _protocolDirector.BroadcastTransaction(tx);
                }
                catch (Exception e)
                {
                    _logger?.Trace("Broadcasting transaction failed: {0},\n{1}", e.Message, tx.GetTransactionInfo());
                    return TxValidation.TxInsertionAndBroadcastingError.BroadCastFailed;
                }

                _logger?.Trace("Broadcasted transaction to peers: " + tx.GetTransactionInfo());
                return TxValidation.TxInsertionAndBroadcastingError.Success;
            }

            _logger?.Trace("Transaction insertion failed:{0}, [{1}]", res, tx.GetTransactionInfo());
            //await _poolService.RemoveAsync(tx.GetHash());
            return res;
        }

        public async Task<Block> GetBlockAtHeight(int height)
        {
            return await _blockManager.GetBlockByHeight(_nodeConfig.ChainId, (ulong)height);
        }

        /// <summary>
        /// return transaction execution result
        /// </summary>
        /// <param name="txHash"></param>
        /// <returns></returns>
        public async Task<TransactionResult> GetTransactionResult(Hash txHash)
        {
            var res = await _transactionResultService.GetResultAsync(txHash);
            return res;
        }

        #region Private Methods for DPoS

        // ReSharper disable once InconsistentNaming
        private void DoDPoSMining(bool doLogsAboutConsensus = true)
        {
            new EventLoopScheduler().Schedule(() =>
            {
                _logger?.Debug("-- DPoS Mining Has been fired!");
                

                //Record the rounds count in local memory
                ulong roundNumber = 0;
                
                //In Value of the BP in one round, will update in every round
                var inValue = Hash.Generate();
                
                //Use this value to make sure every BP produce one block in one timeslot
                ulong latestMinedNormalBlockRoundNumber = 0;
                //Use thisvalue to make sure every EBP produce one block in one timeslot
                ulong latestMinedExtraBlockRoundNumber = 0;
                //Use this value to make sure every BP try once in one timeslot
                ulong latestTriedToHelpProducingExtraBlockRoundNumber = 0;
                //Use this value to make sure every BP try to publish its in value onece in one timeslot
                ulong lastTryToPublishInValueRoundNumber = 0;

                var dPoSInfo = "";

                var intervalSequnce = GetIntervalObservable();
                intervalSequnce.Subscribe
                (
                    async x =>
                    {
                        _logger?.Debug("---- DPoS checking start");
                        var currentHeightOfThisNode = (long) await _chainManager.GetChainCurrentHeight(ChainId);
                        var currentHeightOfOtherNodes = _protocolDirector.GetLatestIndexOfOtherNode();
                        if (currentHeightOfThisNode < currentHeightOfOtherNodes && currentHeightOfOtherNodes != -1)
                        {
                            _logger?.Debug("Current height of me: " + currentHeightOfThisNode);
                            _logger?.Debug("Current height of others: " + currentHeightOfOtherNodes);
                            _logger?.Debug("Having more blocks to sync, so the dpos mining won't start");
                            _logger?.Debug("---- DPoS checking end");
                            return;
                        }

                        var actualRoundNumber = x == 0 ? 0 : _dPoSHelper.CurrentRoundNumber.Value;
                        if (roundNumber != actualRoundNumber)
                        {
                            //Update the rounds count
                            roundNumber = actualRoundNumber;

                            //Update the In Value
                            inValue = Hash.Generate();
                        }

                        #region Try to generate first extra block

                        if (x == 0)
                        {
                            if (!_nodeConfig.ConsensusInfoGenerater)
                            {
                                _logger?.Debug("---- DPoS checking end");
                                return;
                            }
                            
                            await MiningWithInitializingAElfDPoSInformation();

                            var firstBlock =
                                await Mine(); //Which is the first extra block (which can produce DPoS information)

                            await BroadcastBlock(firstBlock);

                            _logger?.Log(LogLevel.Debug,
                                "Generate first extra block: \"{0}\", with [{1}] transactions",
                                firstBlock.GetHash().ToHex(),
                                firstBlock.Body.Transactions.Count);
                            _logger?.Debug("---- DPoS checking end");
                            return;
                        }

                        #endregion

                        #region Log DPoS Info

                        if (doLogsAboutConsensus)
                        {
                            // ReSharper disable once InconsistentNaming
                            var currentDPoSInfo = await GetDPoSInfo(currentHeightOfThisNode);
                            if (dPoSInfo != currentDPoSInfo)
                            {
                                dPoSInfo = currentDPoSInfo;
                                _logger?.Log(LogLevel.Debug, dPoSInfo);
                            }
                        }

                        #endregion

                        #region Try to mine normal block

                        if (latestMinedNormalBlockRoundNumber != roundNumber)
                        {
                            if (await CheckAbleToMineNormalBlock())
                            {
                                var signature = Hash.Default;
                                if (roundNumber > 1)
                                {
                                    signature = await _dPoSHelper.CalculateSignature(inValue);
                                }

                                // out = hash(in)
                                Hash outValue = inValue.CalculateHash();

                                await MiningWithPublishingOutValueAndSignature();

                                var block = await Mine();

                                if (!await BroadcastBlock(block))
                                {
                                    _logger?.Debug("---- DPoS checking end");
                                    return;
                                }

                                latestMinedNormalBlockRoundNumber = roundNumber;

                                _logger?.Log(LogLevel.Debug,
                                    "Generate block: \"{0}\", with [{1}] transactions\n Published out value: {2}\n signature: \"{3}\"",
                                    block.GetHash().Value.ToByteArray().ToHex(), 
                                    block.Body.Transactions.Count,
                                    outValue.Value.ToByteArray().ToHex().Remove(0, 2),
                                    signature.Value.ToByteArray().ToHex().Remove(0, 2));
                                
                                _logger?.Debug("---- DPoS checking end");

                                return;
                            }
                        }

                        #endregion

                        #region Try to mine extra block

                        if (await CheckIsTimeToMineExtraBlock())
                        {
                            //TODO: move this out
                            if (lastTryToPublishInValueRoundNumber != roundNumber)
                            {
                                //Try to publish in value (every BP can do this)
                                await PublishInValue();

                                lastTryToPublishInValueRoundNumber = roundNumber;

                                _logger?.Debug("---- DPoS checking end");

                                return;
                            }

                            if (latestMinedExtraBlockRoundNumber != roundNumber && await CheckAbleToMineExtraBlock())
                            {
                                await MiningWithUpdatingAElfDPoSInformation();

                                var extraBlock = await Mine(); //Which is an extra block

                                if (!await BroadcastBlock(extraBlock))
                                {
                                    _logger?.Debug(extraBlock == null
                                        ? $"Block broadcast failed: failed to mine extra block"
                                        : $"Block broadcast failed: height {extraBlock?.Header.Index}");
                                    _logger?.Debug("---- DPoS checking end");
                                    return;
                                }

                                latestMinedExtraBlockRoundNumber = roundNumber;

                                _logger?.Log(LogLevel.Debug,
                                    "Generate extra block: {0}, with {1} transactions",
                                    extraBlock.GetHash().Value.ToByteArray().ToHex(), 
                                    extraBlock.Body.Transactions.Count);

                                _logger?.Debug("---- DPoS checking end");

                                return;
                            }
                        }

                        #endregion

                        #region Try to help mining extra block

                        if (await CheckAbleToHelpMiningExtraBlock() && 
                            latestTriedToHelpProducingExtraBlockRoundNumber != roundNumber)
                        {
                            await MiningWithUpdatingAElfDPoSInformation();

                            var extraBlock = await Mine(); //Which is an extra block

                            if (!await BroadcastBlock(extraBlock))
                            {
                                _logger?.Debug(extraBlock == null
                                    ? $"Block broadcast failed: failed to mine extra block"
                                    : $"Block broadcast failed: height {extraBlock?.Header.Index}");
                                _logger?.Debug("---- DPoS checking end");
                                return;
                            }
                            
                            latestTriedToHelpProducingExtraBlockRoundNumber = roundNumber;

                            _logger?.Log(LogLevel.Debug,
                                "Help to generate extra block: {0}, with {1} transactions",
                                extraBlock.GetHash().Value.ToByteArray().ToHex(),
                                extraBlock.Body.Transactions.Count);
                            
                        }
                        
                        _logger?.Debug("---- DPoS checking end");

                        #endregion
                    },

                    ex =>
                    {
                        _logger?.Error("Error occurs to dpos part");
                    },

                    () =>
                    {
                        _logger?.Debug("Complete dpos");
                    }
                );
                
            });
        }
            
        private async Task<ITransaction> GenerateTransaction(string methodName, IReadOnlyList<byte[]> parameters)
        {
            var tx = new Transaction
            {
                From = _nodeKeyPair.GetAddress(),
                To = ContractAccountHash,
                IncrementId = await GetIncrementId(_nodeKeyPair.GetAddress()),
                MethodName = methodName,
                P = ByteString.CopyFrom(_nodeKeyPair.PublicKey.Q.GetEncoded())
            };
            
            switch (parameters.Count)
            {
                case 2:
                    tx.Params = ByteString.CopyFrom(ParamsPacker.Pack(parameters[0], parameters[1]));
                    break;
                case 3:
                    tx.Params = ByteString.CopyFrom(ParamsPacker.Pack(parameters[0], parameters[1], parameters[2]));
                    break;
                case 4:
                    tx.Params = ByteString.CopyFrom(ParamsPacker.Pack(parameters[0], parameters[1], parameters[2],
                        parameters[3]));
                    break;
            }
            
            var signer = new ECSigner();
            var signature = signer.Sign(_nodeKeyPair, tx.GetHash().GetHashBytes());

            // Update the signature
            tx.R = ByteString.CopyFrom(signature.R);
            tx.S = ByteString.CopyFrom(signature.S);

            return tx;
        }

        #region Broadcast Txs

        // ReSharper disable once InconsistentNaming
        public async Task MiningWithInitializingAElfDPoSInformation()
        {
            Console.WriteLine(111111111111);
            var parameters = new List<byte[]>
            {
                BlockProducers.ToByteArray(), 
                _dPoSHelper.GenerateInfoForFirstTwoRounds().ToByteArray()
            };
            // ReSharper disable once InconsistentNaming
            var txToInitializeAElfDPoS = await GenerateTransaction(
                "InitializeAElfDPoS",
                parameters);
            await BroadcastTransaction(txToInitializeAElfDPoS);
            
            var block = await Mine();
            await BroadcastBlock(block);
        }

        public async Task MiningWithPublishingOutValueAndSignature()
        {
            var inValue = Hash.Generate();

            if (_consensusData.Count <= 0)
            {
                _consensusData.Push(inValue.CalculateHash());
                _consensusData.Push(inValue);
            }

            var currentRoundNumber = _dPoSHelper.CurrentRoundNumber;
            
            var signature = Hash.Default;
            if (currentRoundNumber.Value > 1)
            {
                signature = await _dPoSHelper.CalculateSignature(inValue);
            }
            
            var parameters = new List<byte[]>
            {
                _dPoSHelper.CurrentRoundNumber.ToByteArray(),
                new StringValue {Value = _nodeKeyPair.GetAddress().ToHex().RemoveHexPrefix()}.ToByteArray(),
                _consensusData.Pop().ToByteArray(),
                signature.ToByteArray()
            };
            
            var txToPublishOutValueAndSignature = await GenerateTransaction(
                "PublishOutValueAndSignature",
                parameters);

            await BroadcastTransaction(txToPublishOutValueAndSignature);

            var block = await Mine();
            await BroadcastBlock(block);
        }

        public async Task PublishInValue()
        {
            var currentRoundNumber = _dPoSHelper.CurrentRoundNumber;

            var parameters = new List<byte[]>
            {
                currentRoundNumber.ToByteArray(),
                new StringValue {Value = _nodeKeyPair.GetAddress().ToHex().RemoveHexPrefix()}.ToByteArray(),
                _consensusData.Pop().ToByteArray()
            };
            
            var txToPublishInValue = await GenerateTransaction(
                "PublishInValue",
                parameters);

            await BroadcastTransaction(txToPublishInValue);
        }

        // ReSharper disable once InconsistentNaming
        public async Task MiningWithUpdatingAElfDPoSInformation()
        {
            var currentRoundNumber = _dPoSHelper.CurrentRoundNumber;

            var extraBlockResult = await ExecuteTxsForExtraBlock();

            var parameters = new List<byte[]>
            {
                currentRoundNumber.ToByteArray(),
                extraBlockResult.Item1.ToByteArray(),
                extraBlockResult.Item2.ToByteArray(),
                extraBlockResult.Item3.ToByteArray()
            };
            
            var txForExtraBlock = await GenerateTransaction(
                "UpdateAElfDPoS",
                parameters);

            await BroadcastTransaction(txForExtraBlock);
            
            var block = await Mine();
            await BroadcastBlock(block);
        }

        #endregion
        
        private async Task<Tuple<RoundInfo, RoundInfo, StringValue>> ExecuteTxsForExtraBlock()
        {
            var currentRoundInfo = await _dPoSHelper.SupplyPreviousRoundInfo();
            var nextRoundInfo = await _dPoSHelper.GenerateNextRoundOrder();
            // ReSharper disable once InconsistentNaming
            var nextEBP = await _dPoSHelper.CalculateNextExtraBlockProducer();
            
            return Tuple.Create(currentRoundInfo, nextRoundInfo, nextEBP);
        }

        // ReSharper disable once InconsistentNaming
        private async Task<string> GetDPoSInfo(long height)
        {
            return await _dPoSHelper.GetDPoSInfoToStringOfLatestRounds(3) + $". Current height: {height}";
        }
        
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private IObservable<long> GetIntervalObservable()
        {
            return Observable.Interval(TimeSpan.FromMilliseconds(CheckTime));
        }

        private async Task<bool> CheckAbleToMineNormalBlock()
        {
            try
            {
                return await _dPoSHelper.AbleToMine();
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to check the ability to mine normal block");
                return false;
            }
        }

        private async Task<bool> CheckAbleToHelpMiningExtraBlock()
        {
            try
            {
                return await _dPoSHelper.ReadyForHelpingProducingExtraBlock();
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to check the ability to help mining extra block");
                return false;
            }
        }

        private async Task<bool> CheckIsTimeToMineExtraBlock()
        {
            try
            {
                return await _dPoSHelper.IsTimeToProduceExtraBlock();
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to check the time to mine extra block");
                return false;
            }
        }

        private async Task<bool> CheckAbleToMineExtraBlock()
        {
            try
            {
                return await _dPoSHelper.AbleToProduceExtraBlock();
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Failed to check the ability to mine extra block");
                return false;
            }
        }
        
        #endregion
    }
}