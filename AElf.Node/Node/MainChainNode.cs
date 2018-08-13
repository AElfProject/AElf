using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common.Attributes;
using AElf.Common.ByteArrayHelpers;
using AElf.Configuration;
using AElf.Cryptography.ECDSA;
using AElf.Execution;
using AElf.Execution.Scheduling;
using AElf.Kernel.Consensus;
using AElf.Kernel.Managers;
using AElf.Kernel.Node.Protocol;
using AElf.Kernel.Types;
using AElf.Network;
using AElf.Network.Connection;
using AElf.Network.Data;
using AElf.Network.Peers;
using AElf.SmartContract;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.Node
{
    [LoggerName("Node")]
    public class MainChainNode : IAElfNode
    {
        private readonly IP2P _p2p;
        public ECKeyPair NodeKeyPair { get; private set; }
        private readonly ITxPoolService _txPoolService;
        private readonly ITransactionManager _transactionManager;
        private readonly ILogger _logger;
        private readonly INodeConfig _nodeConfig;
        private readonly IMiner _miner;
        private readonly IAccountContextService _accountContextService;
        private readonly IBlockVaildationService _blockVaildationService;
        private readonly IChainContextService _chainContextService;
        private readonly IChainService _chainService;
        private readonly IChainCreationService _chainCreationService;
        private readonly IWorldStateDictator _worldStateDictator;
        private readonly ISmartContractService _smartContractService;
        private readonly IFunctionMetadataService _functionMetadataService;
        private readonly INetworkManager _netManager;
        private readonly IBlockSynchronizer _synchronizer;
        private readonly IBlockExecutor _blockExecutor;
        private IConsensus _consensus;
        private MinerHelper _minerHelper;

        public IBlockChain BlockChain { get; }

        public Hash ContractAccountHash => _chainCreationService.GenesisContractHash(_nodeConfig.ChainId, SmartContractType.AElfDPoS);

        public int IsMiningInProcess => _minerHelper.IsMiningInProcess;

        public Hash ChainId => _nodeConfig.ChainId;

        public MainChainNode(ITxPoolService poolService, ITransactionManager txManager,
            ILogger logger,
            INodeConfig nodeConfig, IMiner miner, IAccountContextService accountContextService,
            IBlockVaildationService blockVaildationService,
            IChainContextService chainContextService, IBlockExecutor blockExecutor,
            IChainCreationService chainCreationService, IWorldStateDictator worldStateDictator,
            IChainService chainService, ISmartContractService smartContractService,
            IFunctionMetadataService functionMetadataService, INetworkManager netManager,
            IBlockSynchronizer synchronizer, IP2P p2p)
        {
            _chainCreationService = chainCreationService;
            _chainService = chainService;
            _worldStateDictator = worldStateDictator;
            _smartContractService = smartContractService;
            _functionMetadataService = functionMetadataService;
            _txPoolService = poolService;
            _transactionManager = txManager;
            _logger = logger;
            _nodeConfig = nodeConfig;
            _miner = miner;
            _accountContextService = accountContextService;
            _blockVaildationService = blockVaildationService;
            _chainContextService = chainContextService;
            _worldStateDictator = worldStateDictator;
            _blockExecutor = blockExecutor;
            _netManager = netManager;
            _synchronizer = synchronizer;
            _p2p = p2p;
            BlockChain = _chainService.GetBlockChain(_nodeConfig.ChainId);
        }

        public bool Start(ECKeyPair nodeKeyPair, bool startRpc, int rpcPort, string rpcHost, string initData,
            byte[] tokenContractCode, byte[] consensusContractCode, byte[] basicContractZero)
        {
            SetupConsensus();
            SetupMinerHelper();
            if (_nodeConfig == null)
            {
                _logger?.Log(LogLevel.Error, "No node configuration.");
                return false;
            }

            if (_nodeConfig.ChainId == null || _nodeConfig.ChainId.Length <= 0)
            {
                _logger?.Log(LogLevel.Error, "No chain id.");
                return false;
            }

            try
            {
                _logger?.Log(LogLevel.Debug, "Chain Id = \"{0}\"", _nodeConfig.ChainId.ToHex());
                var genesis = GetGenesisContractHash(SmartContractType.BasicContractZero);
                _logger?.Log(LogLevel.Debug, "Genesis contract address = \"{0}\"", genesis.ToHex());
                    
                var tokenContractAddress = GetGenesisContractHash(SmartContractType.TokenContract);
                _logger?.Log(LogLevel.Debug, "Token contract address = \"{0}\"", tokenContractAddress.ToHex());
                    
                var consensusAddress = GetGenesisContractHash(SmartContractType.AElfDPoS);
                _logger?.Log(LogLevel.Debug, "DPoS contract address = \"{0}\"", consensusAddress.ToHex());
                
                var blockchain = _chainService.GetBlockChain(_nodeConfig.ChainId);
                var curHash = blockchain.GetCurrentBlockHashAsync().Result;
                var chainExists = curHash != null && !curHash.Equals(Hash.Genesis);
                if (!chainExists)
                {
                    // Creation of the chain if it doesn't already exist
                    var tokenSCReg = new SmartContractRegistration
                    {
                        Category = 0,
                        ContractBytes = ByteString.CopyFrom(tokenContractCode),
                        ContractHash = tokenContractCode.CalculateHash(),
                        Type = (int)SmartContractType.TokenContract
                    };
                    
                    var consensusCReg = new SmartContractRegistration
                    {
                        Category = 0,
                        ContractBytes = ByteString.CopyFrom(consensusContractCode),
                        ContractHash = consensusContractCode.CalculateHash(),
                        Type = (int)SmartContractType.AElfDPoS
                    };
                    
                    var basicReg = new SmartContractRegistration
                    {
                        Category = 0,
                        ContractBytes = ByteString.CopyFrom(basicContractZero),
                        ContractHash = basicContractZero.CalculateHash(),
                        Type = (int)SmartContractType.BasicContractZero
                    };
                    var res = _chainCreationService.CreateNewChainAsync(_nodeConfig.ChainId,
                        new List<SmartContractRegistration> {basicReg, tokenSCReg, consensusCReg}).Result;

                    _logger?.Log(LogLevel.Debug, "Genesis block hash = \"{0}\"", res.GenesisBlockHash.ToHex());
                    
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
            }

            // set world state
            _worldStateDictator.SetChainId(_nodeConfig.ChainId);

            NodeKeyPair = nodeKeyPair;

            _txPoolService.Start();

            Task.Run(() => _netManager.Start());

//            _netManager.MessageReceived += ProcessPeerMessage;

            //_protocolDirector.SetCommandContext(this, _nodeConfig.ConsensusInfoGenerater); // If not miner do sync
            if (!_nodeConfig.ConsensusInfoGenerater)
            {
//                _synchronizer.SyncFinished += BlockSynchronizerOnSyncFinished;
                _synchronizer.SyncFinished += (s, e) => { StartMining(); };
            }
            else
            {
                StartMining();
            }

            Task.Run(() => _synchronizer.Start(this, !_nodeConfig.ConsensusInfoGenerater));
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

                _logger?.Log(LogLevel.Debug, "Coinbase = \"{0}\"", _miner.Coinbase.ToHex());
            }

            _logger?.Log(LogLevel.Debug, "AElf node started.");

            Task.Run(async () => await _p2p.ProcessLoop()).ConfigureAwait(false);

            return true;
        }


        private void SetupConsensus()
        {
            if (_consensus != null)
            {
                return;
            }
            switch (Globals.ConsensusType)
            {
                case ConsensusType.AElfDPoS:
                    _consensus=new DPoS(_logger,this,_nodeConfig, _worldStateDictator, _accountContextService, _txPoolService, _p2p);
                    break;
                
                case ConsensusType.PoTC:
                    _consensus=new PoTC(_logger, this, _miner, _accountContextService, _txPoolService, _p2p);
                    break;
                
                case ConsensusType.SingleNode:
                    _consensus=new StandaloneNodeConsensusPlaceHolder(_logger, this, _p2p);
                    break;
            }
        }
        
        private void SetupMinerHelper()
        {
            if (_minerHelper != null)
            {
                return;
            }
            _minerHelper = new MinerHelper(_logger, this, _txPoolService, _nodeConfig,
                _worldStateDictator, _blockExecutor, _chainService, _chainContextService,
                _blockVaildationService, _miner, _consensus, _synchronizer);
        }

        private void StartMining()
        {
            if (_nodeConfig.IsMiner)
            {
                SetupConsensus();
                SetupMinerHelper();
                _consensus?.Start();
            }
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

        public async Task<Hash> GetLastValidBlockHash()
        {
            var pointer = ResourcePath.CalculatePointerForLastBlockHash(_nodeConfig.ChainId);
            return await _worldStateDictator.GetDataAsync(pointer);
        }

        public async Task<BlockExecutionResult> ExecuteAndAddBlock(IBlock block)
        {
            return await _minerHelper.ExecuteAndAddBlock(block);
        }
//        /// <summary>
//        /// Add a new block received from network by first validating it and then
//        /// executing it.
//        /// </summary>
//        /// <param name="block"></param>
//        /// <returns></returns>
//        public async Task<BlockExecutionResult> ExecuteAndAddBlock(IBlock block)
//        {
//            try
//            {
//                var res = Interlocked.CompareExchange(ref _flag, 1, 0);
//                if (res == 1)
//                    return new BlockExecutionResult(false, ValidationError.Mining);
//
//                var context = await _chainContextService.GetChainContextAsync(_nodeConfig.ChainId);
//                var error = await _blockVaildationService.ValidateBlockAsync(block, context, NodeKeyPair);
//
//                if (error != ValidationError.Success)
//                {
//                    var blockchain = _chainService.GetBlockChain(_nodeConfig.ChainId);
//                    var localCorrespondingBlock = await blockchain.GetBlockByHeightAsync(block.Header.Index);
//                    if (error == ValidationError.OrphanBlock)
//                    {
//                        //TODO: limit the count of blocks to rollback
//                        if (block.Header.Time.ToDateTime() < localCorrespondingBlock.Header.Time.ToDateTime())
//                        {
//                            _logger?.Trace("Ready to rollback");
//                            //Rollback world state
//                            var txs = await _worldStateDictator.RollbackToSpecificHeight(block.Header.Index);
//
//                            await _txPoolService.RollBack(txs);
//                            _worldStateDictator.PreBlockHash = block.Header.PreviousBlockHash;
//                            await _worldStateDictator.RollbackCurrentChangesAsync();
//
//                            var ws = await _worldStateDictator.GetWorldStateAsync(block.GetHash());
//                            _logger?.Trace($"Current world state {(await ws.GetWorldStateMerkleTreeRootAsync()).ToHex()}");
//
//                            error = ValidationError.Success;
//                        }
//                        else
//                        {
//                            // insert to database 
//                            Interlocked.CompareExchange(ref _flag, 0, 1);
//                            return new BlockExecutionResult(false, ValidationError.OrphanBlock);
//                        }
//                    }
//                    else
//                    {
//                        Interlocked.CompareExchange(ref _flag, 0, 1);
//                        _logger?.Trace("Invalid block received from network: " + error);
//                        return new BlockExecutionResult(false, error);
//                    }
//                }
//
//                var executed = await _blockExecutor.ExecuteBlock(block);
//                Interlocked.CompareExchange(ref _flag, 0, 1);
//
//                Task.WaitAll();
//                await CheckUpdatingConsensusProcess();
//
//                return new BlockExecutionResult(executed, error);
//                //return new BlockExecutionResult(true, error);
//            }
//            catch (Exception e)
//            {
//                _logger?.Error(e, "Block synchronzing failed");
//                Interlocked.CompareExchange(ref _flag, 0, 1);
//                return new BlockExecutionResult(e);
//            }
//        }

        public Hash GetGenesisContractHash(SmartContractType contractType)
        {
            return _chainCreationService.GenesisContractHash(_nodeConfig.ChainId, contractType);
        }

        public async Task<IBlock> Mine()
        {
            return await _minerHelper.Mine();
        }

        /// <summary>
        /// Broadcasts a transaction to the network. This method
        /// also places it in the transaction pool.
        /// </summary>
        /// <param name="tx">The tx to broadcast</param>
        public async Task<TxValidation.TxInsertionAndBroadcastingError> BroadcastTransaction(ITransaction tx)
        {
            if(tx.From.Equals(NodeKeyPair.GetAddress()))
                _logger?.Trace("Try to insert DPoS transaction to pool: " + tx.GetHash().ToHex() + ", threadId: " +
                           Thread.CurrentThread.ManagedThreadId);
            TxValidation.TxInsertionAndBroadcastingError res;

            var stopWatch = new Stopwatch();
            try
            {
                stopWatch.Start();
                res = await _txPoolService.AddTxAsync(tx);
                stopWatch.Stop();
                //_logger?.Info($"### Debug _txPoolService.AddTxAsync Time: {stopWatch.ElapsedMilliseconds}");
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
                    stopWatch.Start();
                    var transaction = tx.Serialize();
                    await _netManager.BroadcastMessage(MessageType.BroadcastTx, transaction);
                    stopWatch.Stop();
                   // _logger?.Info($"### Debug _netManager.BroadcastMessage Time: {stopWatch.ElapsedMilliseconds}");
                }
                catch (Exception e)
                {
                    _logger?.Trace("Broadcasting transaction failed: {0},\n{1}", e.Message, tx.GetTransactionInfo());
                    return TxValidation.TxInsertionAndBroadcastingError.BroadCastFailed;
                }
                if(tx.From.Equals(NodeKeyPair.GetAddress()))
                    _logger?.Trace("Broadcasted transaction to peers: " + tx.GetTransactionInfo());
                return TxValidation.TxInsertionAndBroadcastingError.Success;
            }

            _logger?.Trace("Transaction insertion failed:{0}, [{1}]", res, tx.GetTransactionInfo());
            // await _poolService.RemoveAsync(tx.GetHash());
            return res;
        }
    }
}