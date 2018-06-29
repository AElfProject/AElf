using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.BlockValidationFilters;
using AElf.Kernel.Concurrency;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Kernel.Concurrency.Scheduling;
using AElf.Kernel.Consensus;
using AElf.Kernel.Managers;
using AElf.Kernel.Miner;
using AElf.Kernel.Node.Config;
using AElf.Kernel.Node.Protocol;
using AElf.Kernel.Node.RPC;
using AElf.Kernel.Node.RPC.DTO;
using AElf.Kernel.Services;
using AElf.Kernel.TxMemPool;
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
        private readonly ITxPoolService _poolService;
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

        private DPoS _dPoS;

        public Hash ContractAccountHash =>
            _chainCreationService.GenesisContractHash(_nodeConfig.ChainId);

        public IExecutive Executive =>
            _smartContractService.GetExecutiveAsync(ContractAccountHash, _nodeConfig.ChainId).Result;
        
        private const int CheckTime = 5000;

        private bool _flag = false;

        public BlockProducer BlockProducers
        {
            get
            {
                var dict = MinersInfo.Instance.Producers;
                var blockProducers = new BlockProducer();
                foreach (var bp in dict.Values)
                {
                    blockProducers.Nodes.Add(bp["address"]);
                }

                return blockProducers;
            }
        }

        public Hash ChainId
        {
            get => _nodeConfig.ChainId;
        }


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
            _poolService = poolService;
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
        }

        public bool Start(ECKeyPair nodeKeyPair, bool startRpc, int confParserRpcPort, string initData, byte[] code)
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
                    
                    _logger.Log(LogLevel.Debug, "Chain Id = \"{0}\"", _nodeConfig.ChainId.Value.ToBase64());
                    _logger.Log(LogLevel.Debug, "Genesis block hash = \"{0}\"", res.GenesisBlockHash.Value.ToBase64());
                    var contractAddress = GetGenesisContractHash();
                    _logger.Log(LogLevel.Debug, "HEX Genesis contract address = \"{0}\"",
                        BitConverter.ToString(contractAddress.ToAccount().Value.ToByteArray()).Replace("-",""));
                    
                    _logger.Log(LogLevel.Debug, "Genesis contract address = \"{0}\"",
                        contractAddress.ToAccount().Value.ToBase64());
                }
            }
            catch (Exception e)
            {
                _logger?.Log(LogLevel.Error, "Could not create the chain : " + _nodeConfig.ChainId.Value.ToBase64());
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
                _rpcServer.Start(confParserRpcPort);

            _poolService.Start();
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
                
                Mine();
                _logger.Log(LogLevel.Debug, "Coinbase = \"{0}\"", _miner.Coinbase.Value.ToByteArray().ToHex());
            }
            
            _logger?.Log(LogLevel.Debug, "AElf node started.");
            
            return true;
        }


        
        private async Task<bool> InitialDebugSync(string initFileName)
        {
            try
            {
                string appFolder = _nodeConfig.DataDir;
                var fullPath = System.IO.Path.Combine(appFolder, "tests", initFileName);

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
                        var address = Convert.FromBase64String(kv.Key);
                        var balance = kv.Value.ToObject<ulong>();
                        
                        var accountDataProvider = await _worldStateDictator.GetAccountDataProvider(address);
                        var dataProvider = accountDataProvider.GetDataProvider();
                        
                        // set balance
                        await dataProvider.SetAsync("Balance".CalculateHash(),
                            new UInt64Value {Value = balance}.ToByteArray());
                        var str = $"Initial balance {balance} in Address \"{kv.Key}\""; 
                        _logger.Log(LogLevel.Debug, "Initial balance {0} in Address \"{1}\"", balance, kv.Key);
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
            if (_poolService.TryGetTx(txId, out var tx))
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
        /// <returns></returns>
        public async Task ReceiveTransaction(ByteString messagePayload, bool isFromSend)
        {
            try
            {
                Transaction tx = Transaction.Parser.ParseFrom(messagePayload);

                //_logger.Trace("Received Transaction: " + Convert.ToBase64String(tx.GetHash().Value.ToByteArray()));
                
                bool success = await _poolService.AddTxAsync(tx);

                if (!success)
                    return;

                if (isFromSend)
                {
                    _protocolDirector.AddTransaction(tx);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Invalid tx - Could not receive transaction from the network", null);
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
                var idInDB = (await _accountContextService.GetAccountDataContext(addr, _nodeConfig.ChainId))
                    .IncrementId;
                var idInPool = _poolService.GetIncrementId(addr);

                return Math.Max(idInDB, idInPool);
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        public async Task<Hash> GetLastValidBlockHash()
        {
            var pointer = Path.CalculatePointerForLastBlockHash(_nodeConfig.ChainId);
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
                var context = await _chainContextService.GetChainContextAsync(_nodeConfig.ChainId);
                var error = await _blockVaildationService.ValidateBlockAsync(block, context);
                Console.WriteLine("try execute block");
                if (error != ValidationError.Success)
                {
                    _logger.Trace("Invalid block received from network" + error.ToString());
                    return new BlockExecutionResult(false, error);
                }

                bool executed = await _blockExecutor.ExecuteBlock(block);

                return new BlockExecutionResult(executed, error);
                //return new BlockExecutionResult(true, error);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Block synchronzing failed");
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
                    if (!_poolService.TryGetTx(id, out var tx))
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
        public async Task<bool> AddTransaction(ITransaction tx)
        {
            return await _poolService.AddTxAsync(tx);
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
            //System.Diagnostics.Debug.WriteLine(ByteString.CopyFrom(code).ToBase64());
            
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
        public async Task Mine()
        {
            await DoDPoSMining();
            /*var txDev = DeployTxDemo(_nodeKeyPair);
            var b1 = await _miner.Mine();
            _logger.Log(LogLevel.Debug,
                "Generated block: {0}, with {1} txs and index {2}, previous block hash: {3}",
                b1.Header.GetHash().Value.ToBase64(), b1.Body.Transactions.Count, b1.Header.Index,
                b1.Header.PreviousBlockHash.Value.ToBase64());
            await Task.Delay(20000);
            
            var devRes = await _transactionResultService.GetResultAsync(txDev.GetHash());
            Hash addr = devRes.RetVal.DeserializeToPbMessage<Hash>();

            var acc1 = Hash.Generate().ToAccount();
            var txInv1 = InvokTxDemo(_nodeKeyPair, addr, "InitializeAsync", ParamsPacker.Pack(acc1, (ulong)101), 1);

            var acc2 = Hash.Generate().ToAccount();
            var txInv2 = InvokTxDemo(_nodeKeyPair, addr, "InitializeAsync", ParamsPacker.Pack(acc2, (ulong)101), 2);
            
            
            var acc3 = Hash.Generate().ToAccount();
            var txInv3 = InvokTxDemo(_nodeKeyPair, addr, "InitializeAsync", ParamsPacker.Pack(acc3, (ulong)101), 3);

            var acc4 = Hash.Generate().ToAccount();
            var txInv4 = InvokTxDemo(_nodeKeyPair, addr, "InitializeAsync", ParamsPacker.Pack(acc4, (ulong)101), 4);
            
            var b2 = await _miner.Mine();
            
            _logger.Log(LogLevel.Debug,
                "Generated block: {0}, with {1} txs and index {2}, previous block hash: {3}",
                b2.Header.GetHash().Value.ToBase64(), b2.Body.Transactions.Count, b2.Header.Index,
                b2.Header.PreviousBlockHash.Value.ToBase64());
            await Task.Delay(20000);
            
            var txInv5 = InvokTxDemo(_nodeKeyPair, addr, "GetBalance", ParamsPacker.Pack(acc1), 5);
            var txInv6 = InvokTxDemo(_nodeKeyPair, addr, "GetBalance", ParamsPacker.Pack(acc2), 6);

            var b3 = await _miner.Mine();
            _logger.Log(LogLevel.Debug,
                "Generated block: {0}, with {1} txs and index {2}, previous block hash: {3}",
                b3.Header.GetHash().Value.ToBase64(), b3.Body.Transactions.Count, b3.Header.Index,
                b3.Header.PreviousBlockHash.Value.ToBase64());
            
            var inv3Res = await _transactionResultService.GetResultAsync(txInv5.GetHash());
            var inv4Res = await _transactionResultService.GetResultAsync(txInv6.GetHash());*/
        }

            


        public async Task<bool> BroadcastBlock(IBlock block)
        {
            int count = 0;
            count = await _protocolDirector.BroadcastBlock(block as Block);

            _logger.Trace("Broadcasted block " + Convert.ToBase64String(block.GetHash().Value.ToByteArray()) + " to " +
                          count + $" peers. Current block height:{block.Header.Index}");

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
        public async Task<bool> BroadcastTransaction(ITransaction tx)
        {
            bool res;

            try
            {
                res = await _poolService.AddTxAsync(tx);
            }
            catch (Exception e)
            {
                _logger.Trace("Pool insertion failed: " + tx.GetHash().Value.ToBase64());
                return false;
            }

            if (res)
            {
                try
                {
                    await _protocolDirector.BroadcastTransaction(tx);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                _logger.Trace("Broadcasted transaction to peers: " + tx.GetTransactionInfo());
                return true;
            }

            _logger.Trace("Broadcasting transaction failed: { txid: " + tx.GetHash().Value.ToBase64() + " }");
            return false;
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
        private async Task DoDPoSMining(bool doLogsAboutConsensus = true)
        {
            await Task.Run(() =>
            {
                _dPoS = new DPoS(_nodeKeyPair);
                    
                //Record the rounds count in local memory
                ulong roundsCount = 0;
                
                //In Value of the BP in one round, will update in every round
                var inValue = Hash.Generate();
                
                //Use this value to make sure every BP produce one block in one timeslot
                ulong latestMinedNormalBlockRoundsCount = 0;
                //Use this value to make sure every EBP produce one block in one timeslot
                ulong latestMinedExtraBlockRoundsCount = 0;
                //Use this value to make sure every BP try once in one timeslot
                ulong latestTriedToHelpProducingExtraBlockRoundsCount = 0;
                
                var dPoSInfo = "";
                
                var intervalSequnce = GetIntervalObservable();
                intervalSequnce.Subscribe
                (
                    async x =>
                    {
                        var actualRoundsCount = await GetActualRoundsCount();
                        if (roundsCount != actualRoundsCount)
                        {
                            //Update the rounds count
                            roundsCount = actualRoundsCount;
                            
                            //Update the In Value
                            inValue = Hash.Generate();
                        }

                        #region Try to generate first extra block

                        if (x == 0)
                        {
                            if (!_nodeConfig.IsChainCreator) 
                                return;

                            var dpoSInfo = await ExecuteTxsForFirstExtraBlock();

                            await BroadcastSyncTxForFirstExtraBlock(dpoSInfo);
                            
                            var firstBlock = await _miner.Mine(); //Which is an extra block

                            await BroadcastBlock(firstBlock);
                            
                            _logger.Log(LogLevel.Debug, "Genereate first extra block: {0}, with {1} transactions, able to mine in {2}", firstBlock.GetHash(),
                                firstBlock.Body.Transactions.Count, DateTime.UtcNow.ToString("u"));

                            return;
                        }

                        #endregion

                        #region Log DPoS Info

                        if (doLogsAboutConsensus)
                        {
                            // ReSharper disable once InconsistentNaming
                            var currentDPoSInfo = await GetDPoSInfo();
                            if (dPoSInfo != currentDPoSInfo)
                            {
                                dPoSInfo = currentDPoSInfo;
                                _logger.Log(LogLevel.Debug, dPoSInfo);
                            }
                        }

                        #endregion
                        
                        #region Try to mine normal block

                        if (latestMinedNormalBlockRoundsCount != roundsCount)
                        {
                            if (await CheckAbleToMineNormalBlock())
                            {
                                var signature = Hash.Default;
                                if (roundsCount > 1)
                                {
                                    signature = await CalculateSignature(inValue);
                                }

                                // out = hash(in)
                                Hash outValue = inValue.CalculateHash();

                                await BroadcastTxsForNormalBlock(roundsCount, outValue, signature, await GetIncrementId(_nodeKeyPair.GetAddress()));

                                var block = await _miner.Mine();
                                
                                await BroadcastBlock(block);
                                
                                latestMinedNormalBlockRoundsCount = roundsCount;

                                #region Do the log for mining normal block
                                
                                _logger.Log(LogLevel.Debug,
                                    "Genereate block: {0}, with {1} transactions, able to mine in {2}\n Published out value: {3}\n signature: {4}",
                                    block.GetHash(), block.Body.Transactions.Count, DateTime.UtcNow.ToString("u"),
                                    outValue.Value.ToBase64(), 
                                    signature.Value.ToBase64());
                                
                                return;

                                #endregion
                            }
                        }

                        #endregion

                        #region Try to mine extra block

                        if (latestMinedExtraBlockRoundsCount != roundsCount && await CheckIsTimeToMineExtraBlock())
                        {
                            var incrementId = await GetIncrementId(_nodeKeyPair.GetAddress());

                            //Try to publish in value (every BP can do this)
                            await BroadcastTransaction(_dPoS.GetTxToPublishInValueTx(
                                incrementId, ContractAccountHash, inValue, new UInt64Value {Value = roundsCount}));

                            latestMinedExtraBlockRoundsCount = roundsCount;

                            if (await CheckAbleToMineExtraBlock())
                            {
                                var extraBlockResult = await ExecuteTxsForExtraBlock(incrementId + 1);

                                await BroadcastTxsToSyncExtraBlock(incrementId + 1, extraBlockResult.Item1, 
                                    extraBlockResult.Item2, extraBlockResult.Item3);

                                var extraBlock = await _miner.Mine(); //Which is an extra block

                                await BroadcastBlock(extraBlock);
                                
                                _logger.Log(LogLevel.Debug,
                                    "Genereate extra block: {0}, with {1} transactions, able to mine in {2}",
                                    extraBlock.GetHash(), extraBlock.Body.Transactions.Count,
                                    DateTime.UtcNow.ToString("u"));
                                return;
                            }
                        }
                        
                        #endregion

                        #region Try to help mining extra block

                        if (latestTriedToHelpProducingExtraBlockRoundsCount != roundsCount && await CheckAbleToHelpMiningExtraBlock())
                        {
                            latestTriedToHelpProducingExtraBlockRoundsCount = roundsCount;

                            var incrementId = await GetIncrementId(_nodeKeyPair.GetAddress());

                            var extraBlockResult = await ExecuteTxsForExtraBlock(incrementId + 1);

                            await BroadcastTxsToSyncExtraBlock(incrementId + 1, extraBlockResult.Item1, 
                                extraBlockResult.Item2, extraBlockResult.Item3);
                            
                            var extraBlock = await _miner.Mine(); //Which is an extra block

                            await BroadcastBlock(extraBlock);


                            #region Broadcast his out value and signature after helping mining extra block

                            var signature = Hash.Default;
                            if (roundsCount > 1)
                            {
                                signature = await CalculateSignature(inValue);
                            }

                            // out = hash(in)
                            Hash outValue = inValue.CalculateHash();
                            
                            await BroadcastTxsForNormalBlock(roundsCount, outValue, signature, incrementId + 2);

                            #endregion

                            _logger.Log(LogLevel.Debug,
                                "Help to genereate extra block: {0}, with {1} transactions, able to mine in {2}",
                                extraBlock.GetHash(), extraBlock.Body.Transactions.Count,
                                DateTime.UtcNow.ToString("u"));
                            return;
                        }

                        #endregion

                        if (doLogsAboutConsensus)
                        {
                            // If this node doesn't produce any block this interval.
                            _logger.Log(LogLevel.Debug, "Find myself unable to mine in {0}", DateTime.UtcNow.ToString("u"));
                        }
                    }
                );
            });
        }

        private async Task<DPoSInfo> ExecuteTxsForFirstExtraBlock()
        {
            var txsForFirstExtraBlock = _dPoS.GetTxsForFirstExtraBlock(
                await GetIncrementId(_nodeKeyPair.GetAddress()), BlockProducers, ContractAccountHash
            );

            var dPoSInfo = new DPoSInfo();
            
            foreach (var tx in txsForFirstExtraBlock)
            {
                var tc = new TransactionContext
                {
                    Transaction = tx
                };
                Executive.SetTransactionContext(tc).Apply(true).Wait();
                
                if (tx.MethodName.StartsWith("Generate"))
                {
                    dPoSInfo = DPoSInfo.Parser.ParseFrom(tc.Trace.RetVal.ToByteArray());
                }
            }

            return dPoSInfo;
        }

        private async Task BroadcastSyncTxForFirstExtraBlock(DPoSInfo dPoSInfo)
        {
            var txToSyncFirstExtraBlock = _dPoS.GetTxToSyncFirstExtraBlock(
                await GetIncrementId(_nodeKeyPair.GetAddress()), ContractAccountHash, dPoSInfo, BlockProducers);

            await BroadcastTransaction(txToSyncFirstExtraBlock);
        }

        private async Task BroadcastTxsForNormalBlock(ulong roundsCount, Hash outValue, Hash signature, ulong incrementId)
        {
            var txForNormalBlock = _dPoS.GetTxsForNormalBlock(
                incrementId, ContractAccountHash, roundsCount,
                outValue, signature);
            foreach (var tx in txForNormalBlock)
            {
                await BroadcastTransaction(tx);
            }
        }
        
        private async Task<Tuple<RoundInfo, RoundInfo, StringValue>> ExecuteTxsForExtraBlock(ulong incrementId)
        {
            var txsForNormalBlock = _dPoS.GetTxsForExtraBlock(
                incrementId, ContractAccountHash);
            
            var currentRoundInfo = new RoundInfo();
            var nextRoundInfo = new RoundInfo();
            // ReSharper disable once InconsistentNaming
            var nextEBP = new StringValue();
            
            foreach (var tx in txsForNormalBlock)
            {
                var tc = new TransactionContext
                {
                    Transaction = tx
                };
                Executive.SetTransactionContext(tc).Apply(true).Wait();

                if (tx.MethodName.StartsWith("Supply"))
                {
                    currentRoundInfo = RoundInfo.Parser.ParseFrom(tc.Trace.RetVal.ToByteArray());
                }
                
                if (tx.MethodName.StartsWith("Generate"))
                {
                    nextRoundInfo = RoundInfo.Parser.ParseFrom(tc.Trace.RetVal.ToByteArray());
                }

                if (tx.MethodName.StartsWith("Set"))
                {
                    nextEBP = StringValue.Parser.ParseFrom(tc.Trace.RetVal.ToByteArray());
                }
            }

            return Tuple.Create(currentRoundInfo, nextRoundInfo, nextEBP);
        }

        // ReSharper disable once InconsistentNaming
        private async Task BroadcastTxsToSyncExtraBlock(ulong incrementId,
            RoundInfo currentRoundInfo, RoundInfo nextRoundInfo, StringValue nextEBP)
        {
            var txForExtraBlock = _dPoS.GetTxToSyncExtraBlock(
                incrementId, ContractAccountHash, currentRoundInfo, nextRoundInfo, nextEBP);

            await BroadcastTransaction(txForExtraBlock);
        }

        private async Task<ulong> GetActualRoundsCount()
        {
            var tcGetRoundsCountTx = new TransactionContext
            {
                Transaction = _dPoS.GetRoundsCountTx(await GetIncrementId(_nodeKeyPair.GetAddress()), ContractAccountHash)
            };
            Executive.SetTransactionContext(tcGetRoundsCountTx).Apply(true).Wait();
            
            if (tcGetRoundsCountTx.Trace.StdErr.IsNullOrEmpty())
            {
                return UInt64Value.Parser.ParseFrom(tcGetRoundsCountTx.Trace.RetVal.ToByteArray()).Value;
            }

            return 0;
        }

        // ReSharper disable once InconsistentNaming
        private async Task<string> GetDPoSInfo()
        {
            // ReSharper disable once InconsistentNaming
            var tcGetDPoSInfo = new TransactionContext
            {
                Transaction = _dPoS.GetDPoSInfoToStringTx(await GetIncrementId(_nodeKeyPair.GetAddress()), ContractAccountHash)
            };
            Executive.SetTransactionContext(tcGetDPoSInfo).Apply(true).Wait();

            if (!tcGetDPoSInfo.Trace.StdErr.IsNullOrEmpty())
            {
                return "";
            }
            
            return StringValue.Parser.ParseFrom(tcGetDPoSInfo.Trace.RetVal.ToByteArray()).Value;
        }
        
        // ReSharper disable once MemberCanBeMadeStatic.Local
        private IObservable<long> GetIntervalObservable()
        {
            return Observable.Interval(TimeSpan.FromMilliseconds(CheckTime));
        }

        private async Task<Hash> CalculateSignature(Hash inValue)
        {
            var tcCalculateSignature = new TransactionContext
            {
                Transaction =
                    _dPoS.GetCalculateSignatureTx(
                        await GetIncrementId(_nodeKeyPair.GetAddress()), ContractAccountHash,
                        inValue)
            };
            Executive.SetTransactionContext(tcCalculateSignature).Apply(true).Wait();
            return Hash.Parser.ParseFrom(tcCalculateSignature.Trace.RetVal.ToByteArray());
        }

        private async Task<bool> CheckAbleToMineNormalBlock()
        {
            var tcAbleToMine = new TransactionContext
            {
                Transaction = _dPoS.GetAbleToMineTx(await GetIncrementId(_nodeKeyPair.GetAddress()),
                    ContractAccountHash)
            };
            Executive.SetTransactionContext(tcAbleToMine).Apply(true).Wait();
                            
            return BoolValue.Parser.ParseFrom(tcAbleToMine.Trace.RetVal).Value;
        }

        private async Task<bool> CheckAbleToHelpMiningExtraBlock()
        {
            var tcAbleToHelp = new TransactionContext
            {
                Transaction = _dPoS.GetReadyForHelpingProducingExtraBlockTx(await GetIncrementId(_nodeKeyPair.GetAddress()),
                    ContractAccountHash)
            };
            Executive.SetTransactionContext(tcAbleToHelp).Apply(true).Wait();
                            
            return BoolValue.Parser.ParseFrom(tcAbleToHelp.Trace.RetVal).Value;
        }

        private async Task<bool> CheckIsTimeToMineExtraBlock()
        {
            // ReSharper disable once InconsistentNaming
            var tcIsTimeToProduceEB = new TransactionContext
            {
                Transaction =
                    _dPoS.GetIsTimeToProduceExtraBlockTx(
                        await GetIncrementId(_nodeKeyPair.GetAddress()), ContractAccountHash)
            };
            Executive.SetTransactionContext(tcIsTimeToProduceEB).Apply(true).Wait();

            // ReSharper disable once InconsistentNaming
            return BoolValue.Parser.ParseFrom(tcIsTimeToProduceEB.Trace.RetVal).Value;
        }

        private async Task<bool> CheckAbleToMineExtraBlock()
        {
            // ReSharper disable once InconsistentNaming
            var tcAbleToProduceEB = new TransactionContext
            {
                Transaction =
                    _dPoS.GetAbleToProduceExtraBlockTx(
                        // This tx won't be broadcasted
                        await GetIncrementId(_nodeKeyPair.GetAddress()), ContractAccountHash)
            };
            Executive.SetTransactionContext(tcAbleToProduceEB).Apply(true).Wait();

            // ReSharper disable once InconsistentNaming
            var res = BoolValue.Parser.ParseFrom(tcAbleToProduceEB.Trace.RetVal).Value;
            return res;
        }

        
        #endregion
    }
}