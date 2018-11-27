using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Common.Attributes;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Kernel;
using AElf.Kernel.EventMessages;
using AElf.Kernel.Node;
using AElf.Miner.Miner;
using AElf.Miner.TxMemPool;
using AElf.Node.EventMessages;
using AElf.Synchronization.BlockSynchronization;
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

        private IBlockChain BlockChain => _blockChain ?? (_blockChain =
                                              _chainService.GetBlockChain(
                                                  Hash.LoadHex(ChainConfig.Instance.ChainId)));

        private readonly IConsensus _consensus;

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

        private byte[] TokenGenesisContractCode
        {
            get
            {
                var contractZeroDllPath =
                    Path.Combine(_assemblyDir, $"{GlobalConfig.GenesisTokenContractAssemblyName}.dll");

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
                var contractZeroDllPath =
                    Path.Combine(_assemblyDir, $"{GlobalConfig.GenesisConsensusContractAssemblyName}.dll");

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
            NodeConfig.Instance.ECKeyPair = conf.KeyPair;

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

                var curHash = BlockChain.GetCurrentBlockHashAsync().Result;

                var chainExistence = curHash != null && !curHash.Equals(Hash.Genesis);

                if (!chainExistence)
                {
                    // Create the chain if it doesn't exist
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

            if (NodeConfig.Instance.WillingToMine)
            {
                _miner.Init();
                _logger?.Debug($"Coinbase = {_miner.Coinbase.DumpHex()}");
                
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

        private Hash ChainId => Hash.LoadHex(ChainConfig.Instance.ChainId);

        private void LogGenesisContractInfo()
        {
            var genesis = ContractHelpers.GetGenesisBasicContractAddress(ChainId);
            _logger?.Debug($"Genesis contract address = {genesis.DumpHex()}");

            var tokenContractAddress = ContractHelpers.GetTokenContractAddress(ChainId);
            _logger?.Debug($"Token contract address = {tokenContractAddress.DumpHex()}");

            var consensusAddress = ContractHelpers.GetConsensusContractAddress(ChainId);
            _logger?.Debug($"DPoS contract address = {consensusAddress.DumpHex()}");

            var sidechainContractAddress = ContractHelpers.GetSideChainContractAddress(ChainId);
            _logger?.Debug($"SideChain contract address = {sidechainContractAddress.DumpHex()}");
        }

        private void CreateNewChain(byte[] tokenContractCode, byte[] consensusContractCode, byte[] basicContractZero,
            byte[] sideChainGenesisContractCode)
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

            var sideChainCReg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(sideChainGenesisContractCode),
                ContractHash = Hash.FromRawBytes(sideChainGenesisContractCode),
                SerialNumber = GlobalConfig.SideChainContract
            };
            var res = _chainCreationService.CreateNewChainAsync(Hash.LoadHex(ChainConfig.Instance.ChainId),
                new List<SmartContractRegistration> {basicReg, tokenCReg, consensusCReg, sideChainCReg}).Result;

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

            var block = (Block) await BlockChain.GetBlockByHeightAsync((ulong) height);
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
            return (int) await BlockChain.GetCurrentBlockHeightAsync();
        }
    }
}