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
using Akka.Dispatch;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.Node
{
    // ReSharper disable InconsistentNaming
    [LoggerName("Node")]
    public class MainChainNode : IAElfNode
    {
        private readonly IP2P _p2p;
        public ECKeyPair NodeKeyPair { get; private set; }
        private readonly ITxPoolService _txPoolService;
        private readonly ITransactionManager _transactionManager;
        private readonly ILogger _logger;
        private readonly IMiner _miner;
        private readonly IAccountContextService _accountContextService;
        private readonly IBlockVaildationService _blockVaildationService;
        private readonly IChainContextService _chainContextService;
        private readonly IChainService _chainService;
        private readonly IChainCreationService _chainCreationService;
        private readonly IStateDictator _stateDictator;
        private readonly ISmartContractService _smartContractService;
        private readonly IFunctionMetadataService _functionMetadataService;
        private readonly INetworkManager _netManager;
        private readonly IBlockSynchronizer _synchronizer;
        private readonly IBlockExecutor _blockExecutor;
        private IConsensus _consensus;
        private MinerHelper _minerHelper;

        public IBlockChain BlockChain { get; }

        public Hash ContractAccountHash =>
            _chainCreationService.GenesisContractHash(ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId), SmartContractType.AElfDPoS);

        public int IsMiningInProcess => _minerHelper.IsMiningInProcess;

        public MainChainNode(ITxPoolService poolService, ITransactionManager txManager,
            ILogger logger, IMiner miner, IAccountContextService accountContextService,
            IBlockVaildationService blockVaildationService,
            IChainContextService chainContextService, IBlockExecutor blockExecutor,
            IChainCreationService chainCreationService, IStateDictator stateDictator,
            IChainService chainService, ISmartContractService smartContractService,
            IFunctionMetadataService functionMetadataService, INetworkManager netManager,
            IBlockSynchronizer synchronizer, IP2P p2p)
        {
            _chainCreationService = chainCreationService;
            _chainService = chainService;
            _stateDictator = stateDictator;
            _smartContractService = smartContractService;
            _functionMetadataService = functionMetadataService;
            _txPoolService = poolService;
            _transactionManager = txManager;
            _logger = logger;
            _miner = miner;
            _accountContextService = accountContextService;
            _blockVaildationService = blockVaildationService;
            _chainContextService = chainContextService;
            _blockExecutor = blockExecutor;
            _netManager = netManager;
            _synchronizer = synchronizer;
            _p2p = p2p;
            BlockChain = _chainService.GetBlockChain(ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId));
        }

        public bool Start(ECKeyPair nodeKeyPair, byte[] tokenContractCode, byte[] consensusContractCode,
            byte[] basicContractZero)
        {
            NodeKeyPair = nodeKeyPair;

            #region setup

            SetupConsensus();
            SetupMinerHelper();
            
            if (string.IsNullOrWhiteSpace(NodeConfig.Instance.ChainId))
            {
                _logger?.Log(LogLevel.Error, "No chain id.");
                return false;
            }

            try
            {
                PrintChainInfo();

                var curHash = BlockChain.GetCurrentBlockHashAsync().Result;
                var chainExists = curHash != null && !curHash.Equals(Hash.Genesis);
                if (!chainExists)
                {
                    // Creation of the chain if it doesn't already exist
                    CreateNewChain(tokenContractCode, consensusContractCode, basicContractZero);
                }
                else
                {
                    var preBlockHash = BlockChain.GetCurrentBlockHashAsync().Result;
                    _stateDictator.SetWorldStateAsync(preBlockHash);

                    _stateDictator.RollbackToPreviousBlock();
                }
            }
            catch (Exception e)
            {
                _logger?.Log(LogLevel.Error,
                    "Could not create the chain : " + NodeConfig.Instance.ChainId);
            }

            // set world state
            _stateDictator.ChainId = ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId);

            #endregion setup

            #region start

            _txPoolService.Start();

            Task.Run(() => _netManager.Start());

            if (!NodeConfig.Instance.ConsensusInfoGenerater)
            {
//                _synchronizer.SyncFinished += BlockSynchronizerOnSyncFinished;
                _synchronizer.SyncFinished += (s, e) => { StartMining(); };
            }
            else
            {
                StartMining();
            }

            Task.Run(() => _synchronizer.Start(this, !NodeConfig.Instance.ConsensusInfoGenerater));

//            var resourceDetectionService = new ResourceUsageDetectionService(_functionMetadataService);
//
//            var grouper = new Grouper(resourceDetectionService, _logger);
            _blockExecutor.Start();
            if (NodeConfig.Instance.IsMiner)
            {
                _miner.Start(nodeKeyPair);

                _logger?.Log(LogLevel.Debug, "Coinbase = \"{0}\"", _miner.Coinbase.ToHex());
            }

            _logger?.Log(LogLevel.Debug, "AElf node started.");
            Task.Run(async () => await _p2p.ProcessLoop()).ConfigureAwait(false);

            #endregion start

            return true;
        }

        #region private methods

        private Hash GetGenesisContractHash(SmartContractType contractType)
        {
            return _chainCreationService.GenesisContractHash(ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId), contractType);
        }

        private void PrintChainInfo()
        {
            _logger?.Log(LogLevel.Debug, "Chain Id = \"{0}\"", NodeConfig.Instance.ChainId);
            var genesis = GetGenesisContractHash(SmartContractType.BasicContractZero);
            _logger?.Log(LogLevel.Debug, "Genesis contract address = \"{0}\"", genesis.ToHex());

            var tokenContractAddress = GetGenesisContractHash(SmartContractType.TokenContract);
            _logger?.Log(LogLevel.Debug, "Token contract address = \"{0}\"", tokenContractAddress.ToHex());

            var consensusAddress = GetGenesisContractHash(SmartContractType.AElfDPoS);
            _logger?.Log(LogLevel.Debug, "DPoS contract address = \"{0}\"", consensusAddress.ToHex());
        }

        private void CreateNewChain(byte[] tokenContractCode, byte[] consensusContractCode, byte[] basicContractZero)
        {
            var tokenCReg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(tokenContractCode),
                ContractHash = tokenContractCode.CalculateHash(),
                Type = (int) SmartContractType.TokenContract
            };

            var consensusCReg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(consensusContractCode),
                ContractHash = consensusContractCode.CalculateHash(),
                Type = (int) SmartContractType.AElfDPoS
            };

            var basicReg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(basicContractZero),
                ContractHash = basicContractZero.CalculateHash(),
                Type = (int) SmartContractType.BasicContractZero
            };
            var res = _chainCreationService.CreateNewChainAsync(ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId),
                new List<SmartContractRegistration> {basicReg, tokenCReg, consensusCReg}).Result;

            _logger?.Log(LogLevel.Debug, "Genesis block hash = \"{0}\"", res.GenesisBlockHash.ToHex());
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
                    _consensus = new DPoS(_logger, this, _stateDictator, _accountContextService,
                        _txPoolService, _p2p);
                    break;

                case ConsensusType.PoTC:
                    _consensus = new PoTC(_logger, this, _miner, _accountContextService, _txPoolService, _p2p);
                    break;

                case ConsensusType.SingleNode:
                    _consensus = new StandaloneNodeConsensusPlaceHolder(_logger, this, _p2p);
                    break;
            }
        }

        private void SetupMinerHelper()
        {
            if (_minerHelper != null)
            {
                return;
            }

            _minerHelper = new MinerHelper(_logger, this, _txPoolService,
                _stateDictator, _blockExecutor, _chainService, _chainContextService,
                _blockVaildationService, _miner, _consensus, _synchronizer);
        }

        private void StartMining()
        {
            if (NodeConfig.Instance.IsMiner)
            {
                SetupConsensus();
                SetupMinerHelper();
                _consensus?.Start();
            }
        }

        #endregion private methods

        #region Legacy Methods

        public async Task<BlockExecutionResult> ExecuteAndAddBlock(IBlock block)
        {
            return await _minerHelper.ExecuteAndAddBlock(block);
        }

        public async Task<IBlock> Mine()
        {
            return await _minerHelper.Mine();
        }

        #endregion Legacy Methods
    }
}