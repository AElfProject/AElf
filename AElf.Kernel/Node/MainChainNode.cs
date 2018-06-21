using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Common.Attributes;
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

namespace AElf.Kernel.Node
{
    [LoggerName("Node")]
    public class MainChainNode : IAElfNode
    {   
        private ECKeyPair _nodeKeyPair;
        private ActorSystem _sys;
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
        private readonly ISynchronizer _synchronizer;
        private readonly IChainManager _chainManager;
        private readonly IChainCreationService _chainCreationService;
        private readonly IWorldStateManager _worldStateManager;
        private readonly ISmartContractService _smartContractService;


        public MainChainNode(ITxPoolService poolService, ITransactionManager txManager, IRpcServer rpcServer, 
            IProtocolDirector protocolDirector, ILogger logger, INodeConfig nodeConfig, IMiner miner, 
            IAccountContextService accountContextService, IBlockVaildationService blockVaildationService,
                ISynchronizer synchronizer, IChainCreationService chainCreationService, 
                IChainContextService chainContextService, IChainManager chainManager, IWorldStateManager worldStateManager, ISmartContractService smartContractService, ActorSystem sys)
        {
            _chainCreationService = chainCreationService;
            _chainManager = chainManager;
            _worldStateManager = worldStateManager;
            _smartContractService = smartContractService;
            _sys = sys;
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
            _synchronizer = synchronizer;
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
                        ContractHash = Hash.Zero
                    };
                    var res = _chainCreationService.CreateNewChainAsync(_nodeConfig.ChainId, smartContractZeroReg)
                        .Result;
                    _logger.Log(LogLevel.Debug, "Chain Id = \"{0}\"", _nodeConfig.ChainId.ToByteString().ToBase64());
                    _logger.Log(LogLevel.Debug, "Genesis block hash = \"{0}\"", res.GenesisBlockHash.Value.ToBase64());
                    var contractAddress = new Hash(_nodeConfig.ChainId.CalculateHashWith("__SmartContractZero__"))
                        .ToAccount();
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
            _protocolDirector.SetCommandContext(this);
            

            if (_nodeConfig.IsMiner)
            {
                // akka env 
                
                IActorRef serviceRouter = _sys.ActorOf(LocalServicesProvider.Props(new ServicePack
                {
                    ChainContextService = _chainContextService,
                    SmartContractService = _smartContractService,
                    ResourceDetectionService = new MockResourceUsageDetectionService()
                }));
                IActorRef generalExecutor = _sys.ActorOf(GeneralExecutor.Props(_sys, serviceRouter), "exec");
                generalExecutor.Tell(new RequestAddChainExecutor(_nodeConfig.ChainId), generalExecutor);
                
                _miner.Start(nodeKeyPair);
                Mine();
                _logger.Log(LogLevel.Debug, "Coinbase = \"{0}\"", _miner.Coinbase.Value.ToStringUtf8());
            }
                  
            
            _logger.Log(LogLevel.Debug, "AElf node started.");
            
            

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
                ;
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
        /// Broadcasts a transaction to the network. This method
        /// also places it in the transaction pool.
        /// </summary>
        /// <param name="tx">The tx to broadcast</param>
        public async Task<bool> BroadcastTransaction(ITransaction tx)
        {
            bool res = true;
            
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
                
                _logger.Trace("Broadcasted transaction to peers: " + tx.GetTransactionInfo().ToString());
                return true;
            }
            
            _logger.Trace("Broadcasting transaction failed: { txid: " + tx.GetHash().Value.ToBase64() + " }");
            return false;
        }
        
        /// <summary>
        /// This method processes a transaction received from one of the
        /// connected peers.
        /// </summary>
        /// <param name="messagePayload"></param>
        /// <returns></returns>
        public async Task ReceiveTransaction(ByteString messagePayload)
        {
            try
            {
                Transaction tx = Transaction.Parser.ParseFrom(messagePayload);
                _logger.Trace("Received Transaction: " + JsonFormatter.Default.Format(tx));
                await _poolService.AddTxAsync(tx);
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
                var idInDB = (await _accountContextService.GetAccountDataContext(addr, _nodeConfig.ChainId)).IncrementId;
                var idInPool = _poolService.GetIncrementId(addr);

                return Math.Max(idInDB, idInPool);
            }
            catch (Exception e)
            {
                return 0;
            }
        }

        /// <summary>
        /// Add a new block received from network
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public async Task<bool> AddBlock(IBlock block)
        {
            try
            {
                var context = await _chainContextService.GetChainContextAsync(_nodeConfig.ChainId);
                var error = await _blockVaildationService.ValidateBlockAsync(block, context);
                if (error != ValidationError.Success)
                {
                    _logger.Trace("Invalid block received from network");
                    return false;
                }
            
                return await _synchronizer.ExecuteBlock(block);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Block synchronzing failed");
                return false;
            }
            
        }
        
        /// <summary>
        /// get missing tx hashes for the block
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public List<Hash> GetMissingTransactions(IBlock block)
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
                while (true)
                {
                    await Task.Delay(15000);
                    var block = await _miner.Mine();
                    _logger.Log(LogLevel.Debug, "Genereate block: {0}, with {1} transactions", block.GetHash(),
                        block.Body.Transactions.Count);
                }
            });
        }
    }
}