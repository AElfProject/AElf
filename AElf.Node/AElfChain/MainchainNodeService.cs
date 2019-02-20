using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Account;
using AElf.Kernel.EventMessages;
using AElf.Kernel.Services;
using AElf.Kernel.Types;
using AElf.Node.Consensus;
using AElf.Node.EventMessages;
using AElf.Synchronization.BlockSynchronization;
using AElf.Synchronization.EventMessages;
using Easy.MessageHub;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Node.AElfChain
{
    // ReSharper disable InconsistentNaming
    public class MainchainNodeService : INodeService, ISingletonDependency
    {
        public ILogger<MainchainNodeService> Logger {get;set;}
        private readonly ITxHub _txHub;
        private readonly IChainService _chainService;
        private readonly IChainCreationService _chainCreationService;
        private readonly IBlockSynchronizer _blockSynchronizer;
        private readonly IAccountService _accountService;
        private readonly IConsensusService _consensusService;

        private IBlockChain _blockChain;
        
        private IConsensus _consensus;

        // todo temp solution because to get the dlls we need the launchers directory (?)
        private string _assemblyDir;

        private bool _forkFlag;
        
        public MainchainNodeService(
            ITxHub hub,
            IChainCreationService chainCreationService,
            IBlockSynchronizer blockSynchronizer,
            IChainService chainService,
            IConsensus consensus,
            IAccountService accountService,
            IConsensusService consensusService)
        {
            _chainCreationService = chainCreationService;
            _chainService = chainService;
            _txHub = hub;
            Logger = NullLogger<MainchainNodeService>.Instance;
            _consensus = consensus;
            _blockSynchronizer = blockSynchronizer;
            _accountService = accountService;
            _consensusService = consensusService;
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

        private byte[] DividendsContractZero => ReadContractCode(GlobalConfig.GenesisDividendsContractAssemblyName);

        private byte[] ReadContractCode(string assemblyName)
        {
            var contractZeroDllPath = Path.Combine(_assemblyDir, $"{assemblyName}.dll");
            return ReadCode(contractZeroDllPath);
        }
        
        private byte[] ReadCode(string path)
        {
            return File.ReadAllBytes(Path.GetFullPath(path));
        }

        #endregion

        public void Initialize(int chainId, NodeConfiguration conf)
        {
            _assemblyDir = conf.LauncherAssemblyLocation;
            _blockChain = _chainService.GetBlockChain(chainId);
                        
            MessageHub.Instance.Subscribe<TxReceived>(async inTx =>
            {
                await _txHub.AddTransactionAsync(chainId, inTx.Transaction);
            });

            _txHub.Initialize(chainId);
        }

        public bool Start(int chainId)
        {
            Logger.LogInformation($"Chain Id = {chainId.DumpBase58()}");

            #region setup

            try
            {
                LogGenesisContractInfo(chainId);

                var curHash = _blockChain.GetCurrentBlockHashAsync().Result;

                var chainExistence = curHash != null && !curHash.Equals(Hash.Genesis);

                if (!chainExistence)
                {
                    // Create the chain if it doesn't exist
                    CreateNewChain(chainId, TokenGenesisContractCode, ConsensusGenesisContractCode, BasicContractZero,
                        CrossChainGenesisContractZero, AuthorizationContractZero, ResourceContractZero, DividendsContractZero);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Could not create the chain : {chainId.DumpBase58()}.");
            }

            #endregion setup

            #region start
            
            _txHub.Start();

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

        public async Task<bool> CheckDPoSAliveAsync()
        {
            return await Task.FromResult(_consensus.IsAlive());
        }

        public async Task<bool> CheckForkedAsync()
        {
            return await Task.FromResult(_forkFlag);
        }

        #region private methods
        
        private void LogGenesisContractInfo(int chainId)
        {
            var genesis = ContractHelpers.GetGenesisBasicContractAddress(chainId);
            Logger.LogDebug($"Genesis contract address = {genesis.GetFormatted()}");

            var tokenContractAddress = ContractHelpers.GetTokenContractAddress(chainId);
            Logger.LogDebug($"Token contract address = {tokenContractAddress.GetFormatted()}");

            var consensusContractAddress = ContractHelpers.GetConsensusContractAddress(chainId);
            Logger.LogDebug($"Consensus contract address = {consensusContractAddress.GetFormatted()}");

            var crosschainContractAddress = ContractHelpers.GetCrossChainContractAddress(chainId);
            Logger.LogDebug($"CrossChain contract address = {crosschainContractAddress.GetFormatted()}");

            var authorizationContractAddress = ContractHelpers.GetAuthorizationContractAddress(chainId);
            Logger.LogDebug($"Authorization contract address = {authorizationContractAddress.GetFormatted()}");

            var resourceContractAddress = ContractHelpers.GetResourceContractAddress(chainId);
            Logger.LogDebug($"Resource contract address = {resourceContractAddress.GetFormatted()}");
            
            var dividendsContractAddress = ContractHelpers.GetDividendsContractAddress(chainId);
            Logger.LogDebug($"Dividends contract address = {dividendsContractAddress.GetFormatted()}");
        }

        private void CreateNewChain(int chainId, byte[] tokenContractCode, byte[] consensusContractCode, byte[]
                basicContractZero, byte[] crossChainGenesisContractCode, byte[] authorizationContractCode,
            byte[] resourceContractCode, byte[] dividendsContractCode)
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
                Category = 2,
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

            var dividendsCReg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(dividendsContractCode),
                ContractHash = Hash.FromRawBytes(dividendsContractCode),
                SerialNumber = GlobalConfig.DividendsContract
            };

            var res = _chainCreationService.CreateNewChainAsync(chainId,
                new List<SmartContractRegistration>
                {
                    basicReg, tokenCReg, consensusCReg, crossChainCReg, authorizationCReg, resourceCReg, dividendsCReg
                }).Result;

            _consensusService.TriggerConsensusAsync(chainId);
            Logger.LogDebug($"Genesis block hash = {res.GenesisBlockHash.ToHex()}");
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
                Logger.LogWarning($"Cannot get block - height {height} is not valid.");
                return null;
            }
            
            var block = (Block) await _blockChain.GetBlockByHeightAsync((ulong)height);
            return block != null ? await FillBlockWithTransactionList(block) : null;
        }

        public async Task<Block> GetBlockFromHash(byte[] hash)
        {
            if (hash == null || hash.Length <= 0)
            {
                Logger.LogWarning("Cannot get block - invalid hash.");
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