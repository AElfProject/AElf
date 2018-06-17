using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.BlockValidationFilters;
using AElf.Kernel.Managers;
using AElf.Kernel.Miner;
using AElf.Kernel.Node.Config;
using AElf.Kernel.Node.Protocol;
using AElf.Kernel.Node.RPC;
using AElf.Kernel.Node.RPC.DTO;
using AElf.Kernel.Services;
using AElf.Kernel.TxMemPool;
using AElf.Network.Data;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using NLog;
using NLog.Common;

namespace AElf.Kernel.Node
{
    [LoggerName("Node")]
    public class MainChainNode : IAElfNode
    {   
        private ECKeyPair _nodeKeyPair;
        
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
        private readonly IBlockManager _blockManager;
        private readonly IChainManager _chainManager;
        private readonly IChainCreationService _chainCreationService;

        public MainChainNode(ITxPoolService poolService, ITransactionManager txManager, IRpcServer rpcServer, 
            IProtocolDirector protocolDirector, ILogger logger, INodeConfig nodeConfig, IMiner miner, 
            IAccountContextService accountContextService, IBlockVaildationService blockVaildationService, 
            IChainContextService chainContextService, IBlockManager blockManager, IChainManager chainManager,
            IChainCreationService chainCreationService)
        {
            _chainCreationService = chainCreationService;
            _chainManager = chainManager;
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
        }

        public bool Start(ECKeyPair nodeKeyPair, bool startRpc, string initdata)
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
                    var smartContractZeroReg = new SmartContractRegistration { Category = 0, ContractHash = Hash.Zero };
                    var res = _chainCreationService.CreateNewChainAsync(_nodeConfig.ChainId, smartContractZeroReg).Result;
                }
            }
            catch (Exception e)
            {
                _logger?.Log(LogLevel.Error, "Could not create the chain : " + _nodeConfig.ChainId.Value.ToBase64());
            }
            
            
            if (!string.IsNullOrWhiteSpace(initdata))
            {
                if (!InitialDebugSync(initdata))
                {
                    //todo log 
                    return false;
                }
            }
            
            _nodeKeyPair = nodeKeyPair;
            
            if (startRpc)
                _rpcServer.Start();
            
            _poolService.Start();
            _protocolDirector.Start();
            
            // todo : avoid circular dependency
            _rpcServer.SetCommandContext(this);
            _protocolDirector.SetCommandContext(this);
            
            if(_nodeConfig.IsMiner)
                _miner.Start();    
            
            _logger.Log(LogLevel.Debug, "AElf node started.");
            if (_nodeConfig.IsMiner)
            {
                _logger.Log(LogLevel.Debug, "Chain Id = \"{0}\"", _nodeConfig.ChainId.ToByteString().ToBase64());
                _logger.Log(LogLevel.Debug, "Coinbase = \"{0}\"", _miner.Coinbase.Value.ToStringUtf8());
            }

            return true;
        }

        private bool InitialDebugSync(string initFileName)
        {
            try
            {
                string appFolder = _nodeConfig.DataDir;
                var fullPath = System.IO.Path.Combine(appFolder, "tests", initFileName);

                Block b = null;
                using (StreamReader r = new StreamReader(fullPath))
                {
                    string jsonChain = r.ReadToEnd();
                    b = JsonParser.Default.Parse<Block>(jsonChain);
                }

                if (b != null)
                {
                    var r = _chainManager.AppendBlockToChainAsync(b).Result;
                    var bd = _blockManager.AddBlockAsync(b).Result;
                    
                    
                    Block blockOfHeight1 = _blockManager.GetBlockByHeight(_nodeConfig.ChainId, 1).Result;
                    
                }
                else
                {
                    ;
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
                
                _logger.Trace("Broadcasted transaction to peers: " + tx.GetTransactionInfo());
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
        /// validate a new block received
        /// </summary>
        /// <param name="block"></param>
        /// <returns></returns>
        public async Task<ValidationError> ValidateBlock(IBlock block)
        {
            var context = await _chainContextService.GetChainContextAsync(_nodeConfig.ChainId);
            var error = await _blockVaildationService.ValidateBlockAsync(block, context);
            return error;
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
    }
}