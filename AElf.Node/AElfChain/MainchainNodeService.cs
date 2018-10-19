using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.EventMessages;
using AElf.ChainController.TxMemPool;
using AElf.Common;
using AElf.Common.Attributes;
using AElf.Common.Enums;
using AElf.Configuration;
using AElf.Configuration.Config.Consensus;
using AElf.Kernel;
using AElf.Kernel.Node;
using AElf.Miner.Miner;
using AElf.Node.Protocol;
using AElf.SmartContract;
using Google.Protobuf;
using NLog;
using ServiceStack;
using AElf.Node.EventMessages;
using Easy.MessageHub;

namespace AElf.Node.AElfChain
{
    // ReSharper disable InconsistentNaming
    [LoggerName("Node")]
    public class MainchainNodeService : INodeService
    {
        private readonly ILogger _logger;

        private readonly ITxPoolService _txPoolService;
        private readonly IMiner _miner;
        private readonly IP2P _p2p;
        private readonly IAccountContextService _accountContextService;
        private readonly IBlockValidationService _blockValidationService;
        private readonly IChainContextService _chainContextService;
        private readonly IChainService _chainService;
        private readonly IChainCreationService _chainCreationService;
        private readonly IStateDictator _stateDictator;
        private readonly IBlockSyncService _blockSyncService;
        private readonly IBlockExecutionService _blockExecutionService;
        private readonly TxHub _txHub;

        private IBlockChain _blockChain;
        private IConsensus _consensus;

        // todo temp solution because to get the dlls we need the launchers directory (?)
        private string _assemblyDir;

        public MainchainNodeService(ITxPoolService poolService,
            IAccountContextService accountContextService,
            IBlockValidationService blockValidationService,
            IChainContextService chainContextService,
            IChainCreationService chainCreationService,
            IBlockSyncService blockSyncService,
            IStateDictator stateDictator,
            IChainService chainService,
            IBlockExecutor blockExecutor,
            IMiner miner,
            TxHub txHub,
            IP2P p2p,
            IBlockExecutionService blockExecutionService,
            ILogger logger)
        {
            _chainCreationService = chainCreationService;
            _chainService = chainService;
            _stateDictator = stateDictator;
            _txPoolService = poolService;
            _logger = logger;
            _blockExecutionService = blockExecutionService;
            _miner = miner;
            _txHub = txHub;
            _p2p = p2p;
            _accountContextService = accountContextService;
            _blockValidationService = blockValidationService;
            _chainContextService = chainContextService;
            _stateDictator = stateDictator;
            _blockSyncService = blockSyncService;
        }

        #region Genesis Contracts

        private byte[] TokenGenesisContractCode
        {
            get
            {
                var contractZeroDllPath
                    = Path.Combine(_assemblyDir, $"{GlobalConfig.GenesisTokenContractAssemblyName}.dll");

                byte[] code;
                using (var file = File.OpenRead(Path.GetFullPath(contractZeroDllPath)))
                {
                    code = file.ReadFully();
                }

                return code;
            }
        }

        private byte[] ConsensusGenesisContractCode
        {
            get
            {
                var contractZeroDllPath
                    = Path.Combine(_assemblyDir, $"{GlobalConfig.GenesisConsensusContractAssemblyName}.dll");

                byte[] code;
                using (var file = File.OpenRead(Path.GetFullPath(contractZeroDllPath)))
                {
                    code = file.ReadFully();
                }

                return code;
            }
        }

        private byte[] BasicContractZero
        {
            get
            {
                var contractZeroDllPath =
                    Path.Combine(_assemblyDir, $"{GlobalConfig.GenesisSmartContractZeroAssemblyName}.dll");

                byte[] code;
                using (var file = File.OpenRead(Path.GetFullPath(contractZeroDllPath)))
                {
                    code = file.ReadFully();
                }

                return code;
            }
        }

        private byte[] SideChainGenesisContractZero
        {
            get
            {
                var contractZeroDllPath =
                    Path.Combine(_assemblyDir, $"{GlobalConfig.GenesisSideChainContractAssemblyName}.dll");

                byte[] code;
                using (var file = File.OpenRead(Path.GetFullPath(contractZeroDllPath)))
                {
                    code = file.ReadFully();
                }

                return code;
            }
        }

        #endregion

        public void Initialize(NodeConfiguration conf)
        {
            _assemblyDir = conf.LauncherAssemblyLocation;
            _blockChain = _chainService.GetBlockChain(Hash.LoadHex(NodeConfig.Instance.ChainId));
            NodeConfig.Instance.ECKeyPair = conf.KeyPair;

            _txHub.CurrentHeightGetter = () => _blockChain.GetCurrentBlockHeightAsync().Result;
            MessageHub.Instance.Subscribe<BlockHeader>((bh) => _txHub.OnNewBlockHeader(bh));
            SetupConsensus();

            MessageHub.Instance.Subscribe<TxReceived>(async inTx =>
            {
                await _txPoolService.AddTxAsync(inTx.Transaction);
            });

            MessageHub.Instance.Subscribe<UpdateConsensus>(option =>
            {
                if (option == UpdateConsensus.Update)
                {
                    _logger?.Trace("Will update consensus.");
                    _consensus?.Update();
                }

                if (option == UpdateConsensus.Dispose)
                {
                    _logger?.Trace("Will stop mining.");
                    _consensus?.Stop();
                }
            });

            MessageHub.Instance.Subscribe<SyncStateChanged>(inState =>
            {
                if (inState.IsSyncing)
                {
                    _logger?.Warn("Will hang on mining due to starting syncing.");
                    _consensus?.Hang();
                }
                else
                {
                    _logger?.Trace("Will start / recover mining.");
                    _consensus?.Start();
                }
            });
        }

        public bool Start()
        {
            if (string.IsNullOrWhiteSpace(NodeConfig.Instance.ChainId))
            {
                _logger?.Log(LogLevel.Error, "No chain id.");
                return false;
            }

            _logger?.Log(LogLevel.Debug, $"Chain Id = {NodeConfig.Instance.ChainId}");

            MessageHub.Instance.Subscribe<BlockReceived>(async inBlock =>
            {
                await _blockSyncService.ReceiveBlock(inBlock.Block);
            });

            MessageHub.Instance.Subscribe<BlockMined>(inBlock => { _blockSyncService.AddMinedBlock(inBlock.Block); });

            #region setup

            try
            {
                LogGenesisContractInfo();

                var curHash = _blockChain.GetCurrentBlockHashAsync().Result;

                var chainExists = curHash != null && !curHash.Equals(Hash.Genesis);

                if (!chainExists)
                {
                    // Creation of the chain if it doesn't already exist
                    CreateNewChain(TokenGenesisContractCode, ConsensusGenesisContractCode, BasicContractZero,
                        SideChainGenesisContractZero);
                }
                else
                {
                    _stateDictator.BlockHeight = _blockChain.CurrentBlock.Header.Index;
                    _stateDictator.BlockProducerAccountAddress = Address.Zero;
                    _stateDictator.SetWorldStateAsync();

                    _stateDictator.RollbackToPreviousBlock();
                }
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Could not create the chain : " + NodeConfig.Instance.ChainId);
            }

            // set world state
            _stateDictator.ChainId = Hash.LoadHex(NodeConfig.Instance.ChainId);

            #endregion setup

            #region start

            _txPoolService.Start();
            _blockExecutionService.Init();

            if (NodeConfig.Instance.IsMiner)
            {
                _miner.Init();

                _logger?.Log(LogLevel.Debug, "Coinbase = \"{0}\"", _miner.Coinbase.DumpHex());
            }

            // todo maybe move
            Task.Run(async () => await _p2p.ProcessLoop()).ConfigureAwait(false);

            Thread.Sleep(1000);

            if (NodeConfig.Instance.ConsensusInfoGenerator)
            {
                StartMining();
                // Start directly.
                _consensus?.Start();
            }

            #endregion start

            return true;
        }

        public void Stop()
        {
            //todo   
        }

        public bool IsDPoSAlive()
        {
            return _consensus.IsAlive();
        }

        // TODO: 
        public bool IsForked()
        {
            return false;
        }

        #region private methods

        private Address GetGenesisContractHash(SmartContractType contractType)
        {
            return _chainCreationService.GenesisContractHash(Hash.LoadHex(NodeConfig.Instance.ChainId), contractType);
        }

        private void LogGenesisContractInfo()
        {
            var genesis = GetGenesisContractHash(SmartContractType.BasicContractZero);
            _logger?.Log(LogLevel.Debug, "Genesis contract address = \"{0}\"", genesis.DumpHex());

            var tokenContractAddress = GetGenesisContractHash(SmartContractType.TokenContract);
            _logger?.Log(LogLevel.Debug, "Token contract address = \"{0}\"", tokenContractAddress.DumpHex());

            var consensusAddress = GetGenesisContractHash(SmartContractType.AElfDPoS);
            _logger?.Log(LogLevel.Debug, "DPoS contract address = \"{0}\"", consensusAddress.DumpHex());

            var sidechainContractAddress = GetGenesisContractHash(SmartContractType.SideChainContract);
            _logger?.Log(LogLevel.Debug, "SideChain contract address = \"{0}\"", sidechainContractAddress.DumpHex());
        }

        private void CreateNewChain(byte[] tokenContractCode, byte[] consensusContractCode, byte[] basicContractZero,
            byte[] sideChainGenesisContractCode)
        {
            var tokenCReg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(tokenContractCode),
                ContractHash = Hash.FromRawBytes(tokenContractCode),
                Type = (int) SmartContractType.TokenContract
            };

            var consensusCReg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(consensusContractCode),
                ContractHash = Hash.FromRawBytes(consensusContractCode),
                Type = (int) SmartContractType.AElfDPoS
            };

            var basicReg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(basicContractZero),
                ContractHash = Hash.FromRawBytes(basicContractZero),
                Type = (int) SmartContractType.BasicContractZero
            };

            var sideChainCReg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(sideChainGenesisContractCode),
                ContractHash = Hash.FromRawBytes(sideChainGenesisContractCode),
                Type = (int) SmartContractType.SideChainContract
            };
            var res = _chainCreationService.CreateNewChainAsync(Hash.LoadHex(NodeConfig.Instance.ChainId),
                new List<SmartContractRegistration> {basicReg, tokenCReg, consensusCReg, sideChainCReg}).Result;

            _logger?.Log(LogLevel.Debug, "Genesis block hash = \"{0}\"", res.GenesisBlockHash.DumpHex());
        }

        private void SetupConsensus()
        {
            if (_consensus != null)
            {
                _logger?.Trace("Consensus has already initialized.");
                return;
            }

            switch (ConsensusConfig.Instance.ConsensusType)
            {
                case ConsensusType.AElfDPoS:
                    _consensus = new DPoS(_stateDictator, _txPoolService, _miner, _chainService);
                    break;

                case ConsensusType.PoTC:
                    _consensus = new PoTC(_miner, _txPoolService);
                    break;

                case ConsensusType.SingleNode:
                    _consensus = new StandaloneNodeConsensusPlaceHolder();
                    break;
            }
        }

        private void StartMining()
        {
            if (NodeConfig.Instance.IsMiner)
            {
                SetupConsensus();
                _consensus?.Start();
            }

//            if (NodeConfig.Instance.IsMiner)
//            {
//                _miner.Init(_nodeKeyPair);
//                _logger?.Log(LogLevel.Debug, "Coinbase = \"{0}\"", _miner.Coinbase.DumpHex());
//                SetupConsensus();
//                _consensus?.Start();
//            }
//            _blockExecutor.FinishInitialSync();
        }

        #endregion private methods

        public int GetCurrentHeight()
        {
            int height = 1;

            try
            {
                height = (int) _blockChain.GetCurrentBlockHeightAsync().Result;
            }
            catch (Exception e)
            {
                _logger?.Trace(e, "Exception while getting chain height");
            }

            return height;
        }
    }
}