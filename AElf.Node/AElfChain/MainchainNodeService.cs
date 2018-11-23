using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Common.Attributes;
using AElf.Common.Enums;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Configuration.Config.Consensus;
using AElf.Kernel;
using AElf.Kernel.Node;
using AElf.Kernel.Storages;
using AElf.Miner.EventMessages;
using AElf.Miner.Miner;
using AElf.Miner.TxMemPool;
using AElf.Node.EventMessages;
using AElf.Synchronization.BlockExecution;
using AElf.Synchronization.BlockSynchronization;
using AElf.Synchronization.EventMessages;
using Base58Check;
using Easy.MessageHub;
using Google.Protobuf;
using NLog;
using ServiceStack;

namespace AElf.Node.AElfChain
{
    // ReSharper disable InconsistentNaming
    [LoggerName("Node")]
    public class MainchainNodeService : INodeService
    {
        private readonly ILogger _logger;

        private readonly ITxHub _txHub;
        private readonly IStateStore _stateStore;
        private readonly IMiner _miner;
        private readonly IChainService _chainService;
        private readonly IChainCreationService _chainCreationService;
        private readonly IBlockSynchronizer _blockSynchronizer;

        private IBlockChain _blockChain;
        private IConsensus _consensus;

        // todo temp solution because to get the dlls we need the launchers directory (?)
        private string _assemblyDir;

        public MainchainNodeService(
            IStateStore stateStore,
            ITxHub hub,
            IChainCreationService chainCreationService,
            IBlockSynchronizer blockSynchronizer,
            IChainService chainService,
            IMiner miner,
            ILogger logger)
        {
            _stateStore = stateStore;
            _chainCreationService = chainCreationService;
            _chainService = chainService;
            _txHub = hub;
            _logger = logger;
            _miner = miner;
            _blockSynchronizer = blockSynchronizer;
        }

        #region Genesis Contracts

        private byte[] TokenGenesisContractCode
        {
            get
            {
                var contractZeroDllPath = Path.Combine(_assemblyDir, $"{GlobalConfig.GenesisTokenContractAssemblyName}.dll");

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
                var contractZeroDllPath = Path.Combine(_assemblyDir, $"{GlobalConfig.GenesisConsensusContractAssemblyName}.dll");

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
                var contractZeroDllPath = Path.Combine(_assemblyDir, $"{GlobalConfig.GenesisSmartContractZeroAssemblyName}.dll");

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
                var contractZeroDllPath = Path.Combine(_assemblyDir, $"{GlobalConfig.GenesisSideChainContractAssemblyName}.dll");

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
            _blockChain = _chainService.GetBlockChain(Hash.LoadBase58(ChainConfig.Instance.ChainId));
            NodeConfig.Instance.ECKeyPair = conf.KeyPair; // todo config should not be set here 

            SetupConsensus();

            MessageHub.Instance.Subscribe<TxReceived>(async inTx =>
            {
                await _txHub.AddTransactionAsync(inTx.Transaction);
            });

            _txHub.Initialize();
        }

        public bool Start()
        {
            if (string.IsNullOrWhiteSpace(ChainConfig.Instance.ChainId))
            {
                _logger?.Error("No chain id.");
                return false;
            }

            _logger?.Info($"Chain Id = {ChainConfig.Instance.ChainId}");

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
            }
            catch (Exception e)
            {
                _logger?.Error(e, $"Could not create the chain : {ChainConfig.Instance.ChainId}.");
            }

            #endregion setup

            #region start

            _txHub.Start();

            if (NodeConfig.Instance.IsMiner)
            {
                _miner.Init();
            }

            Thread.Sleep(1000);

            if (NodeConfig.Instance.ConsensusInfoGenerator)
            {
                StartMining();
                // Start directly.
                _consensus?.Start();
            }

            MessageHub.Instance.Subscribe<BlockReceived>(async inBlock =>
            {
                await _blockSynchronizer.ReceiveBlock(inBlock.Block);
            });

            #endregion start

            MessageHub.Instance.Publish(new ChainInitialized(null));

            return true;
        }

        public bool Stop()
        {
            //todo
            return true;
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
            byte[] chainIdBytes = Hash.LoadBase58(ChainConfig.Instance.ChainId).DumpByteArray();
            byte[] toHash = ByteArrayHelpers.Combine(chainIdBytes, Encoding.UTF8.GetBytes(contractType.ToString()));
            var hash = SHA256.Create().ComputeHash(SHA256.Create().ComputeHash(toHash));

            return Address.FromPublicKey(chainIdBytes, hash);
        }

        private void LogGenesisContractInfo()
        {
            var genesis = GetGenesisContractHash(SmartContractType.BasicContractZero);
            _logger?.Debug($"Genesis contract address = {genesis.GetFormatted()}");

            var tokenContractAddress = GetGenesisContractHash(SmartContractType.TokenContract);
            _logger?.Debug($"Token contract address = {tokenContractAddress.GetFormatted()}");

            var consensusAddress = GetGenesisContractHash(SmartContractType.AElfDPoS);
            _logger?.Debug($"DPoS contract address = {consensusAddress.GetFormatted()}");

            var sidechainContractAddress = GetGenesisContractHash(SmartContractType.SideChainContract);
            _logger?.Debug($"SideChain contract address = {sidechainContractAddress.GetFormatted()}");
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
            var res = _chainCreationService.CreateNewChainAsync(Hash.LoadBase58(ChainConfig.Instance.ChainId),
                new List<SmartContractRegistration> {basicReg, tokenCReg, consensusCReg, sideChainCReg}).Result;

            _logger?.Debug($"Genesis block hash = {res.GenesisBlockHash.DumpHex()}");
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
                    _consensus = new DPoS(_stateStore, _txHub, _miner, _chainService, _blockSynchronizer);
                    break;

                case ConsensusType.PoTC:
                    _consensus = new PoTC(_miner, _txHub);
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
        }

        #endregion private methods

        public async Task<BlockHeaderList> GetBlockHeaderList(ulong index, int count)
        {
            return await _blockSynchronizer.GetBlockHeaderList(index, count);
        }
        
        public async Task<Block> GetBlockAtHeight(int height)
        {
            if (height <= 0)
            {
                _logger?.Warn($"Cannot get block - height {height} is not valid.");
                return null;
            }
            
            var block = (Block) await _chainService.GetBlockChain(Hash.Default).GetBlockByHeightAsync((ulong)height);
            return block != null ? await FillBlockWithTransactionList(block) : null;
        }
        
        public async Task<Block> GetBlockFromHash(byte[] hash)
        {
            if (hash == null || hash.Length <= 0)
            {
                _logger?.Warn("Cannot get block - invalid hash.");
                return null;
            }
            
            return await GetBlockFromHash(Hash.LoadByteArray(hash));
        }
        
        public async Task<Block> GetBlockFromHash(Hash hash)
        {
            var block = await Task.Run(() => (Block) _blockSynchronizer.GetBlockByHash(hash));
            return block != null ? await FillBlockWithTransactionList(block) : null;
        }
        
        private async Task<Block> FillBlockWithTransactionList(Block block)
        {
            block.Body.TransactionList.Clear();
            foreach (var txId in block.Body.Transactions)
            {
                var r = await _txHub.GetReceiptAsync(txId);
                block.Body.TransactionList.Add(r.Transaction);
            }

            return block;
        }

        public async Task<int> GetCurrentBlockHeightAsync()
        {
             return (int) await _blockChain.GetCurrentBlockHeightAsync();
        }
    }
}