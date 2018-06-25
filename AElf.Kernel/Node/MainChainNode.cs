using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Common.ByteArrayHelpers;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.BlockValidationFilters;
using AElf.Kernel.Concurrency;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Extensions;
using AElf.Kernel.Managers;
using AElf.Kernel.Miner;
using AElf.Kernel.Node.Config;
using AElf.Kernel.Node.Protocol;
using AElf.Kernel.Node.RPC;
using AElf.Kernel.Node.RPC.DTO;
using AElf.Kernel.Services;
using AElf.Kernel.TxMemPool;
using AElf.Network.Data;
using Akka.Actor;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Common;
using ServiceStack;

namespace AElf.Kernel.Node
{
    [LoggerName("Node")]
    public class MainChainNode : IAElfNode
    {   
        private ECKeyPair _nodeKeyPair;
        private ActorSystem _sys ;
        
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
        private readonly IWorldStateManager _worldStateManager;
        private readonly ISmartContractService _smartContractService;
        private readonly ITransactionResultService _transactionResultService;

        private readonly IBlockExecutor _blockExecutor;

        public MainChainNode(ITxPoolService poolService, ITransactionManager txManager, IRpcServer rpcServer,
            IProtocolDirector protocolDirector, ILogger logger, INodeConfig nodeConfig, IMiner miner,
            IAccountContextService accountContextService, IBlockVaildationService blockVaildationService,
            IChainContextService chainContextService, IBlockExecutor blockExecutor,
            IChainCreationService chainCreationService, IWorldStateManager worldStateManager, 
            IChainManager chainManager, ISmartContractService smartContractService, ActorSystem sys, 
            ITransactionResultService transactionResultService, IBlockManager blockManager)
        {
            _chainCreationService = chainCreationService;
            _chainManager = chainManager;
            _worldStateManager = worldStateManager;
            _smartContractService = smartContractService;
            _sys = sys;
            _transactionResultService = transactionResultService;
            _blockManager = blockManager;
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
            _worldStateManager = worldStateManager;
            _blockExecutor = blockExecutor;
        }

        public bool Start(ECKeyPair nodeKeyPair, bool startRpc, string initdata, byte[] code = null)
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
                    _logger.Log(LogLevel.Debug, "Genesis block hash = \"{0}\"", BitConverter.ToString(res.GenesisBlockHash.Value.ToByteArray()).Replace("-",""));
                    var contractAddress = new Hash(_nodeConfig.ChainId.CalculateHashWith("__SmartContractZero__"))
                        .ToAccount();
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
            
            
            if (!string.IsNullOrWhiteSpace(initdata))
            {
                /*if (!InitialDebugSync(initdata).Result)
                {
                    //todo log 
                    return false;
                }*/
            }
            
            _nodeKeyPair = nodeKeyPair;

            if (startRpc)
                _rpcServer.Start();

            _poolService.Start();
            _protocolDirector.Start();

            // todo : avoid circular dependency
            _rpcServer.SetCommandContext(this);
            _protocolDirector.SetCommandContext(this, !_nodeConfig.IsMiner); // If not miner do sync
            
            if (_nodeConfig.IsMiner)
            {
                // akka env 
                
//                IActorRef serviceRouter = _sys.ActorOf(LocalServicesProvider.Props(new ServicePack
//                {
//                    ChainContextService = _chainContextService,
//                    SmartContractService = _smartContractService,
//                    ResourceDetectionService = new MockResourceUsageDetectionService()
//                }));
//                IActorRef generalExecutor = _sys.ActorOf(GeneralExecutor.Props(_sys, serviceRouter), "exec");
//                generalExecutor.Tell(new RequestAddChainExecutor(_nodeConfig.ChainId));
                
                _miner.Start(nodeKeyPair);
                
                DeployTxDemo();
                
                Mine();
                
                _logger.Log(LogLevel.Debug, "Coinbase = \"{0}\"", _miner.Coinbase.Value.ToStringUtf8());
            }
            
            _logger?.Log(LogLevel.Debug, "AElf node started.");
            
            return true;
        }


        private ITransaction DeployTxDemo()
        {
            var ContractName = "AElf.Kernel.Tests.TestContract";
            var contractZeroDllPath = $"../{ContractName}/bin/Debug/netstandard2.0/{ContractName}.dll";
            
            byte[] code = null;
            using (FileStream file = File.OpenRead(System.IO.Path.GetFullPath(contractZeroDllPath)))
            {
                code = file.ReadFully();
            }
            //System.Diagnostics.Debug.WriteLine(ByteString.CopyFrom(code).ToBase64());
            ECKeyPair keyPair = new KeyPairGenerator().Generate();
            ECSigner signer = new ECSigner();
            var txDep = new Transaction
            {
                From = keyPair.GetAddress(),
                To = new Hash(_nodeConfig.ChainId.CalculateHashWith("__SmartContractZero__")).ToAccount(),
                IncrementId = 0,
                MethodName = "DeploySmartContract",
                Params = ByteString.CopyFrom(new Parameters()
                {
                    Params = {
                        new Param
                        {
                            IntVal = 0
                        }, 
                        new Param
                        {
                            BytesVal = ByteString.CopyFrom(code)
                        }
                    }
                }.ToByteArray()),
                
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
                        
                        await _worldStateManager.OfChain(_nodeConfig.ChainId);
            
                        var accountDataProvider = _worldStateManager.GetAccountDataProvider(address);
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
                _logger.Trace("Received Transaction: " + JsonFormatter.Default.Format(tx));

                await _poolService.AddTxAsync(tx);

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
            return await _worldStateManager.GetDataAsync(pointer);
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

                if (error != ValidationError.Success)
                {
                    _logger.Trace("Invalid block received from network" + error.ToString());
                    return new BlockExecutionResult(false, error);
                }

                bool executed = await _blockExecutor.ExecuteBlock(block);

                return new BlockExecutionResult(executed, error);
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

        /// <summary>
        /// temple mine to generate fake block data with loop
        /// </summary>
        public void Mine()
        {
            Task.Run(async () =>
            {
                var tx = DeployTxDemo();
                var b = await _miner.Mine();
                var result = await _transactionResultService.GetResultAsync(tx.GetHash());
                //Console.WriteLine(result.RetVal.d);
                _logger.Log(LogLevel.Debug, "Genereate block: {0}, with {1} transactions", b.GetHash(),
                    b.Body.Transactions.Count);
                while (true)
                {
                    await Task.Delay(5000);

                    try
                    {
                        while (true)
                        {
                            await Task.Delay(10000); // secs

                            var block = await _miner.Mine();

                            _logger.Log(LogLevel.Debug, "Genereated block: {0}, with {1} transactions", block.GetHash(),
                                block.Body.Transactions.Count);

                            await BroadcastBlock(block);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Log(LogLevel.Debug, e);
                    }
                }
            });

        }

        public async Task<bool> BroadcastBlock(IBlock block)
        {
            try
            {
                await _protocolDirector.BroadcastBlock(block as Block);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            _logger.Trace("Broadcasted block to peers:");

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
        
    }
}