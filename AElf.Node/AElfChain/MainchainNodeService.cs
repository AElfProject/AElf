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
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.EventMessages;
using AElf.Kernel.Node;
using AElf.Kernel.Storages;
using AElf.Miner.Miner;
using AElf.Miner.TxMemPool;
using AElf.Node.EventMessages;
using AElf.Synchronization.BlockSynchronization;
using AElf.Synchronization.EventMessages;
using Base58Check;
using AElf.Synchronization.EventMessages;
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
        private readonly IMiner _miner;
        private readonly IChainService _chainService;
        private readonly IChainCreationService _chainCreationService;
        private readonly IBlockSynchronizer _blockSynchronizer;

        private IBlockChain _blockChain;
        
        private IConsensus _consensus;

        private ECKeyPair _nodeKeyPair;

        // todo temp solution because to get the dlls we need the launchers directory (?)
        private string _assemblyDir;

        private bool _forkFlag;
        
        public MainchainNodeService(
            ITxHub hub,
            IChainCreationService chainCreationService,
            IBlockSynchronizer blockSynchronizer,
            IChainService chainService,
            IMiner miner,
            IConsensus consensus,
            ILogger logger
            )
        {
            _chainCreationService = chainCreationService;
            _chainService = chainService;
            _txHub = hub;
            _logger = logger;
            _consensus = consensus;
            _miner = miner;
            _blockSynchronizer = blockSynchronizer;
        }

        #region Genesis Contracts

        private byte[] TokenGenesisContractCode => ReadContractCode(GlobalConfig.GenesisTokenContractAssemblyName);

        private byte[] ConsensusGenesisContractCode =>
            ReadContractCode(GlobalConfig.GenesisConsensusContractAssemblyName);

        private byte[] BasicContractZero => ReadContractCode(GlobalConfig.GenesisSmartContractZeroAssemblyName);

        private byte[] CrossChainGenesisContractZero =>
            ReadContractCode(GlobalConfig.GenesisCrossChainContractAssemblyName);

        private byte[] AuthorizationContractZero =>
            ReadContractCode(GlobalConfig.GenesisAuthorizationContractAssemblyName);

        private byte[] ResourceContractZero => ReadContractCode(GlobalConfig.GenesisResourceContractAssemblyName);

        private byte[] ReadContractCode(string assemblyName)
        {
            var contractZeroDllPath = Path.Combine(_assemblyDir, $"{assemblyName}.dll");
            return ReadCode(contractZeroDllPath);
        }
        
        private byte[] ReadCode(string path)
        {
            byte[] code;
            using (var file = File.OpenRead(Path.GetFullPath(path)))
            {
                code = file.ReadFully();
            }

            return code;
        }

        #endregion

        public void Initialize(NodeConfiguration conf)
        {
            _assemblyDir = conf.LauncherAssemblyLocation;
            _blockChain = _chainService.GetBlockChain(Hash.LoadBase58(ChainConfig.Instance.ChainId));
            
            NodeConfig.Instance.ECKeyPair = conf.KeyPair; // todo config should not be set here 
            _nodeKeyPair = conf.KeyPair;
            
            MessageHub.Instance.Subscribe<TxReceived>(async inTx =>
            {
                await _txHub.AddTransactionAsync(inTx.Transaction);
            });

            _txHub.Initialize();
            _miner.Init(_nodeKeyPair);
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

                var chainExistence = curHash != null && !curHash.Equals(Hash.Genesis);

                if (!chainExistence)
                {
                    // Create the chain if it doesn't exist
                    CreateNewChain(TokenGenesisContractCode, ConsensusGenesisContractCode, BasicContractZero,
                        CrossChainGenesisContractZero, AuthorizationContractZero, ResourceContractZero);
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
                _consensus?.Start();
            }

            MessageHub.Instance.Subscribe<BlockReceived>(async inBlock =>
            {
                await _blockSynchronizer.ReceiveBlock(inBlock.Block);
            });

            MessageHub.Instance.Subscribe<BranchedBlockReceived>(inBranchedBlock => { _forkFlag = true; });
            MessageHub.Instance.Subscribe<RollBackStateChanged>(inRollbackState => { _forkFlag = false; });

            #endregion start

            MessageHub.Instance.Publish(new ChainInitialized());

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

        public bool IsForked()
        {
            return _forkFlag;
        }

        #region private methods

        private Hash ChainId => Hash.LoadBase58(ChainConfig.Instance.ChainId);

        private void LogGenesisContractInfo()
        {
            var genesis = ContractHelpers.GetGenesisBasicContractAddress(ChainId);
            _logger?.Debug($"Genesis contract address = {genesis.GetFormatted()}");

            var tokenContractAddress = ContractHelpers.GetTokenContractAddress(ChainId);
            _logger?.Debug($"Token contract address = {tokenContractAddress.GetFormatted()}");

            var consensusAddress = ContractHelpers.GetConsensusContractAddress(ChainId);
            _logger?.Debug($"DPoS contract address = {consensusAddress.GetFormatted()}");

            var crosschainContractAddress = ContractHelpers.GetCrossChainContractAddress(ChainId);
            _logger?.Debug($"CrossChain contract address = {crosschainContractAddress.GetFormatted()}");

            var authorizationContractAddress = ContractHelpers.GetAuthorizationContractAddress(ChainId);
            _logger?.Debug($"Authorization contract address = {authorizationContractAddress.GetFormatted()}");

            var resourceContractAddress = ContractHelpers.GetResourceContractAddress(ChainId);
            _logger?.Debug($"Resource contract address = {resourceContractAddress.GetFormatted()}");
        }

        private void CreateNewChain(byte[] tokenContractCode, byte[] consensusContractCode, byte[] basicContractZero,
            byte[] crossChainGenesisContractCode, byte[] authorizationContractCode, byte[] resourceContractCode)
        {
            var tokenCReg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(tokenContractCode),
                ContractHash = Hash.FromRawBytes(tokenContractCode),
                SerialNumber = GlobalConfig.TokenContract
            };

            var consensusCReg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(consensusContractCode),
                ContractHash = Hash.FromRawBytes(consensusContractCode),
                SerialNumber = GlobalConfig.ConsensusContract
            };

            var basicReg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(basicContractZero),
                ContractHash = Hash.FromRawBytes(basicContractZero),
                SerialNumber = GlobalConfig.GenesisBasicContract
            };

            var crossChainCReg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(crossChainGenesisContractCode),
                ContractHash = Hash.FromRawBytes(crossChainGenesisContractCode),
                SerialNumber = GlobalConfig.CrossChainContract
            };
            
            var authorizationCReg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(authorizationContractCode),
                ContractHash = Hash.FromRawBytes(authorizationContractCode),
                SerialNumber = GlobalConfig.AuthorizationContract
            };
            
            var resourceCReg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(resourceContractCode),
                ContractHash = Hash.FromRawBytes(resourceContractCode),
                SerialNumber = GlobalConfig.ResourceContract
            };
            var res = _chainCreationService.CreateNewChainAsync(Hash.LoadBase58(ChainConfig.Instance.ChainId),
                new List<SmartContractRegistration>
                    {basicReg, tokenCReg, consensusCReg, crossChainCReg, authorizationCReg, resourceCReg}).Result;
            _logger?.Debug($"Genesis block hash = {res.GenesisBlockHash.DumpHex()}");
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
            
            var block = (Block) await _blockChain.GetBlockByHeightAsync((ulong)height);
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
            if (block == null)
                return null;

            if (block.Body.TransactionList.Count > 0)
                return block;

            return await FillBlockWithTransactionList(block);
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