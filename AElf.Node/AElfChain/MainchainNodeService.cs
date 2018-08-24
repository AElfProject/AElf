using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.TxMemPool;
using AElf.Common.Attributes;
using AElf.Common.ByteArrayHelpers;
using AElf.Configuration;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Node.Protocol;
using AElf.Kernel.Types;
using AElf.Miner.Miner;
using AElf.Node;
using AElf.Node.AElfChain;
using AElf.SmartContract;
using Google.Protobuf;
using NLog;
using ServiceStack;

namespace AElf.Kernel.Node
{
    [LoggerName("Node")]
    public class MainchainNodeService : INodeService
    {
        private readonly ILogger _logger;
        
        private readonly ITxPoolService _txPoolService;
        private readonly IMiner _miner;
        private readonly IP2P _p2p;
        private readonly IAccountContextService _accountContextService;
        private readonly IBlockVaildationService _blockVaildationService;
        private readonly IChainContextService _chainContextService;
        private readonly IChainService _chainService;
        private readonly IChainCreationService _chainCreationService;
        private readonly IWorldStateDictator _worldStateDictator;
        private readonly IBlockSynchronizer _synchronizer;
        private readonly IBlockExecutor _blockExecutor;
        
        private IBlockChain _blockChain;
        private IConsensus _consensus;
        
        private ECKeyPair _nodeKeyPair;

        // todo temp solution because to get the dlls we need the launchers directory (?)
        private string _assemblyDir;

        public MainchainNodeService(ITxPoolService poolService, 
            IAccountContextService accountContextService,
            IBlockVaildationService blockVaildationService,
            IChainContextService chainContextService,
            IChainCreationService chainCreationService, 
            IWorldStateDictator worldStateDictator,
            IChainService chainService,
            IBlockExecutor blockExecutor,
            IBlockSynchronizer synchronizer,            
            IMiner miner,
            IP2P p2p,
            ILogger logger)
        {
            _chainCreationService = chainCreationService;
            _chainService = chainService;
            _worldStateDictator = worldStateDictator;
            _txPoolService = poolService;
            _logger = logger;
            _miner = miner;
            _p2p = p2p;
            _accountContextService = accountContextService;
            _blockVaildationService = blockVaildationService;
            _chainContextService = chainContextService;
            _worldStateDictator = worldStateDictator;
            _blockExecutor = blockExecutor;
            _synchronizer = synchronizer;
        }

        #region Genesis Contracts

        private byte[] TokenGenesisContractCode
        {
            get
            {
                var contractZeroDllPath 
                    = Path.Combine(_assemblyDir, $"{Globals.GenesisTokenContractAssemblyName}.dll");

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
                    = Path.Combine(_assemblyDir, $"{Globals.GenesisConsensusContractAssemblyName}.dll");

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
                    Path.Combine(_assemblyDir, $"{Globals.GenesisSmartContractZeroAssemblyName}.dll");

                byte[] code;
                using (var file = File.OpenRead(Path.GetFullPath(contractZeroDllPath)))
                {
                    code = file.ReadFully();
                }

                return code;
            }
        }

        #endregion
        
        public void Initialize(NodeConfiguation conf)
        {
            _nodeKeyPair = conf.KeyPair;
            _assemblyDir = conf.LauncherAssemblyLocation;
            _blockChain = _chainService.GetBlockChain(ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId));
            
            SetupConsensus();
        }

        public bool Start()
        {
            if (string.IsNullOrWhiteSpace(NodeConfig.Instance.ChainId))
            {
                _logger?.Log(LogLevel.Error, "No chain id.");
                return false;
            }
            
            _logger?.Log(LogLevel.Debug, $"Chain Id = {NodeConfig.Instance.ChainId}");
            
            #region setup

            try
            {
                LogGenesisContractInfo();

                var curHash = _blockChain.GetCurrentBlockHashAsync().Result;
                
                var chainExists = curHash != null && !curHash.Equals(Hash.Genesis);
                
                if (!chainExists)
                {
                    // Creation of the chain if it doesn't already exist
                    CreateNewChain(TokenGenesisContractCode, ConsensusGenesisContractCode, BasicContractZero);
                }
                else
                {
                    var preBlockHash = _blockChain.GetCurrentBlockHashAsync().Result;
                    
                    _worldStateDictator.SetWorldStateAsync(preBlockHash);
                    _worldStateDictator.PreBlockHash = preBlockHash;
                    _worldStateDictator.RollbackCurrentChangesAsync();
                }
            }
            catch (Exception e)
            {
                _logger?.Log(LogLevel.Error, "Could not create the chain : " + NodeConfig.Instance.ChainId);
            }

            // set world state
            _worldStateDictator.SetChainId(ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId));

            #endregion setup

            #region start

            _txPoolService.Start();
            Task.Run(() => _synchronizer.Start(this, !NodeConfig.Instance.ConsensusInfoGenerater));
            
            _blockExecutor.Start();
            
            if (NodeConfig.Instance.IsMiner)
            {
                _miner.Start(_nodeKeyPair);

                _logger?.Log(LogLevel.Debug, "Coinbase = \"{0}\"", _miner.Coinbase.ToHex());
            }
            
            // todo maybe move
            Task.Run(async () => await _p2p.ProcessLoop()).ConfigureAwait(false);
            
            Thread.Sleep(1000);
            
            if (!NodeConfig.Instance.ConsensusInfoGenerater)
            {
                _synchronizer.SyncFinished += (s, e) => { StartMining(); };
            }
            else
            {
                StartMining();
            }
            
            #endregion start

            return true;
        }

        public void Stop()
        {
         //todo   
        }
        
        #region private methods

        private Hash GetGenesisContractHash(SmartContractType contractType)
        {
            return _chainCreationService.GenesisContractHash(ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId), contractType);
        }

        private void LogGenesisContractInfo()
        {
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
                return;

            switch (Globals.ConsensusType)
            {
                case ConsensusType.AElfDPoS:
                {
                    var dpos = new DPoS(_logger, _worldStateDictator, _accountContextService, _txPoolService, _p2p, _miner, _blockChain, _synchronizer);
                    var genesisContractHash = _chainCreationService.GenesisContractHash(ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId), SmartContractType.AElfDPoS);
                    dpos.Initialize(genesisContractHash, _nodeKeyPair);
                    _consensus = dpos;
                }
                break;

                case ConsensusType.PoTC:
                    _consensus = new PoTC(_logger, _miner, _accountContextService, _txPoolService, _p2p);
                    break;

                case ConsensusType.SingleNode:
                    _consensus = new StandaloneNodeConsensusPlaceHolder(_logger, _p2p);
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
        }

        #endregion private methods

        #region Legacy Methods
        
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
                var context = await _chainContextService.GetChainContextAsync(ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId));
                var error = await _blockVaildationService.ValidateBlockAsync(block, context, _nodeKeyPair);

                if (error != ValidationError.Success)
                {
                    var blockchain = _chainService.GetBlockChain(ByteArrayHelpers.FromHexString(NodeConfig.Instance.ChainId));
                    var localCorrespondingBlock = await blockchain.GetBlockByHeightAsync(block.Header.Index);
                    if (error == ValidationError.OrphanBlock)
                    {
                        //TODO: limit the count of blocks to rollback
                        if (block.Header.Time.ToDateTime() < localCorrespondingBlock.Header.Time.ToDateTime())
                        {
                            var txs = await _blockChain.RollbackToHeight(block.Header.Index - 1);
                            await _worldStateDictator.RollbackToBlockHash(block.Header.PreviousBlockHash);

                            await _txPoolService.RollBack(txs);
                            _worldStateDictator.PreBlockHash = block.Header.PreviousBlockHash;
                            await _worldStateDictator.RollbackCurrentChangesAsync();

                            var ws = await _worldStateDictator.GetWorldStateAsync(block.GetHash());
                            _logger?.Trace(
                                $"Current world state {(await ws.GetWorldStateMerkleTreeRootAsync()).ToHex()}");

                            error = ValidationError.Success;
                        }
                        else
                        {
                            return new BlockExecutionResult(false, ValidationError.OrphanBlock);
                        }
                    }
                    else
                    {
                        _logger?.Trace("Invalid block received from network: " + error);
                        return new BlockExecutionResult(false, error);
                    }
                }

                var executed = await _blockExecutor.ExecuteBlock(block);

                Task.WaitAll();
                await _consensus.Update();

                return new BlockExecutionResult(executed, error);
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Block synchronzing failed");
                return new BlockExecutionResult(e);
            }
        }

        #endregion Legacy Methods
    }
}