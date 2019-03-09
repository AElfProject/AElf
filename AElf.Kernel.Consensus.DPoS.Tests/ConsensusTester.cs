using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.Genesis;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.ChainController.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.DPoS.Application;
using AElf.Kernel.Consensus.Infrastructure;
using AElf.Kernel.EventMessages;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Node.Application;
using AElf.Kernel.Node.Domain;
using AElf.Kernel.Services;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS.Node.Application;
using AElf.Types.CSharp;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Volo.Abp;
using Volo.Abp.Threading;

namespace AElf.Kernel.Consensus.DPoS.Tests
{
    // ReSharper disable InconsistentNaming
    public class ConsensusTester
    {
        public int ChainId { get; }
        public ECKeyPair CallOwnerKeyPair { get; private set; }
        
        private readonly IConsensusService _consensusService;
        private readonly IAccountService _accountService;
        private readonly IBlockchainService _blockchainService;
        private readonly IOsBlockchainNodeContextService _osBlockchainNodeContextService;
        private readonly IBlockchainExecutingService _blockchainExecutingService;
        private readonly IBlockGenerationService _blockGenerationService;
        private readonly ISystemTransactionGenerationService _systemTransactionGenerationService;
        private readonly IBlockExecutingService _blockExecutingService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IBlockExtraDataService _blockExtraDataService;

        public Chain Chain => AsyncHelper.RunSync(GetChainAsync);

        public bool ScheduleTriggered { get; set; }

        //TODO: should use one ConsensusTester,you can make ge generic type with XXXTestAElfModule, and mock different 
        //services in the module ConfigServices
        public ConsensusTester(int chainId, ECKeyPair callOwnerKeyPair, List<ECKeyPair> initialMinersKeyPairs,
            bool isBootMiner = false)
        {
            ChainId = (chainId == 0) ? ChainHelpers.ConvertBase58ToChainId("AELF") : chainId;
            CallOwnerKeyPair = callOwnerKeyPair ?? CryptoHelpers.GenerateKeyPair();

            var application =
                AbpApplicationFactory.Create<DPoSConsensusTestAElfModule>(options =>
                {
                    options.UseAutofac();
                    options.Services.Configure<ChainOptions>(o => o.ChainId = ChainId);
                    
                    options.Services.AddTransient<IAccountService>(o =>
                    {
                        var mockAccountService = new Mock<IAccountService>();
                        mockAccountService.Setup(s => s.GetPublicKeyAsync()).ReturnsAsync(CallOwnerKeyPair.PublicKey);
                
                        return mockAccountService.Object;
                    });
                });
            application.Initialize();

            var transactionExecutingService = application.ServiceProvider.GetService<ITransactionExecutingService>();
            var transactionReadOnlyExecutionService =
                application.ServiceProvider.GetService<ITransactionReadOnlyExecutionService>();
            //_blockchainNodeContextService = application.ServiceProvider.GetService<IBlockchainNodeContextService>();
            _blockchainService = application.ServiceProvider.GetService<IBlockchainService>();
            var chainManager = application.ServiceProvider.GetService<IChainManager>();
            var blockManager = application.ServiceProvider.GetService<IBlockManager>();
            var blockchainStateManager = application.ServiceProvider.GetService<IBlockchainStateManager>();
            var txHub = application.ServiceProvider.GetService<ITxHub>();
            _smartContractAddressService =
                application.ServiceProvider.GetRequiredService<ISmartContractAddressService>();
            _blockExtraDataService = application.ServiceProvider.GetService<IBlockExtraDataService>();

            _accountService = application.ServiceProvider.GetRequiredService<IAccountService>();
            // Mock dpos options.
            var consensusOptions = MockDPoSOptions(initialMinersKeyPairs, isBootMiner);

            //TODO: if you want to mock a service, just config services in DPoSConsensusTestAElfModule,
            //do not new anything. you can also create XXXDPoSConsensusTestAElfModule depended on DPoSConsensusTestAElfModule,
            //and create application XXXDPoSConsensusTestAElfModule
            var consensusControlInformation = new ConsensusControlInformation();
            _consensusService = new ConsensusService(
                new DPoSInformationGenerationService(consensusOptions, _accountService, consensusControlInformation),
                _accountService, transactionReadOnlyExecutionService, MockConsensusScheduler(), _blockchainService,
                consensusControlInformation, _smartContractAddressService);

            _blockGenerationService = new BlockGenerationService(
                new BlockExtraDataService(new List<IBlockExtraDataProvider>
                    {new ConsensusExtraDataProvider(_consensusService)}),
                application.ServiceProvider.GetRequiredService<IStaticChainInformationProvider>());

            _blockchainExecutingService = new FullBlockchainExecutingService(chainManager, _blockchainService,
                new BlockValidationService(new List<IBlockValidationProvider>
                    {new ConsensusValidationProvider(_consensusService, _blockExtraDataService)}),
                _blockExecutingService);

            _systemTransactionGenerationService = new SystemTransactionGenerationService(
                new List<ISystemTransactionGenerator> {new ConsensusTransactionGenerator(_consensusService)});

            _blockExecutingService = new BlockExecutingService(transactionExecutingService, blockManager,
                blockchainStateManager, _blockGenerationService);

            _osBlockchainNodeContextService =
                application.ServiceProvider.GetRequiredService<IOsBlockchainNodeContextService>();

            InitialChain();
        }

        public async Task TriggerConsensusAsync()
        {
            await _consensusService.TriggerConsensusAsync();
        }

        public async Task<bool> ValidateConsensusBeforeExecutionAsync(byte[] consensusInformation)
        {
            return await _consensusService.ValidateConsensusBeforeExecutionAsync(Chain.BestChainHash, Chain.BestChainHeight,
                consensusInformation);
        }
        
        public async Task<bool> ValidateConsensusAfterExecutionAsync(byte[] consensusInformation)
        {
            return await _consensusService.ValidateConsensusAfterExecutionAsync(Chain.BestChainHash, Chain.BestChainHeight,
                consensusInformation);
        }

        public async Task<byte[]> GetNewConsensusInformationAsync()
        {
            return await _consensusService.GetNewConsensusInformationAsync();
        }

        public async Task<IEnumerable<Transaction>> GenerateConsensusTransactionsAsync()
        {
            return await _consensusService.GenerateConsensusTransactionsAsync();
        }

        public async Task<Block> TimeToMineABlock(List<Transaction> txs)
        {
            if (!ScheduleTriggered)
            {
                throw new InvalidOperationException();
            }

            var preBlock = await _blockchainService.GetBestChainLastBlock();
            var minerService = BuildMinerService(txs);
            return await minerService.MineAsync(preBlock.GetHash(), preBlock.Height,
                DateTime.UtcNow.AddMilliseconds(4000));
        }

        #region Private methods

        private MinerService BuildMinerService(List<Transaction> txs)
        {
            var mockTxHub = new Mock<ITxHub>();
            mockTxHub.Setup(h => h.GetExecutableTransactionSetAsync()).ReturnsAsync(
                new ExecutableTransactionSet
                {
                    PreviousBlockHash = Chain.BestChainHash,
                    PreviousBlockHeight = Chain.BestChainHeight,
                    Transactions = txs
                });

            return new MinerService(_accountService, _blockGenerationService,
                _systemTransactionGenerationService, _blockchainService, _blockExecutingService, _consensusService,
                _blockchainExecutingService, mockTxHub.Object);
        }

        private async Task<Chain> GetChainAsync()
        {
            return await _blockchainService.GetChainAsync();
        }

        private void InitialChain()
        {
            var dto = new OsBlockchainNodeContextStartDto
            {
                ChainId = ChainId,
                ZeroSmartContract = typeof(BasicContractZero)
            };

            dto.InitializationSmartContracts.AddConsensusSmartContract<ConsensusContract>();


            AsyncHelper.RunSync(() => _osBlockchainNodeContextService.StartAsync(dto));
        }

        private ISystemTransactionGenerationService MockSystemTransactionGenerationService(List<Transaction> systemTxs)
        {
            var mockSystemTransactionGenerationService = new Mock<ISystemTransactionGenerationService>();
            mockSystemTransactionGenerationService.Setup(s =>
                    s.GenerateSystemTransactions(It.IsAny<Address>(), It.IsAny<long>(), It.IsAny<Hash>()))
                .Returns(systemTxs);

            return mockSystemTransactionGenerationService.Object;
        }

        private IOptionsSnapshot<DPoSOptions> MockDPoSOptions(List<ECKeyPair> initialMinersKeyPairs, bool isBootMiner)
        {
            var consensusOptionsMock = new Mock<IOptionsSnapshot<DPoSOptions>>();
            consensusOptionsMock.Setup(m => m.Value).Returns(new DPoSOptions
            {
                InitialMiners = initialMinersKeyPairs.Select(p => p.PublicKey.ToHex()).ToList(),
                IsBootMiner = isBootMiner,
                MiningInterval = DPoSConsensusConsts.MiningInterval
            });

            return consensusOptionsMock.Object;
        }

        private IConsensusScheduler MockConsensusScheduler()
        {
            var consensusSchedulerMock = new Mock<IConsensusScheduler>();
            consensusSchedulerMock.Setup(m => m.NewEvent(It.IsNotIn(int.MaxValue), It.IsAny<BlockMiningEventData>()))
                .Callback(() => ScheduleTriggered = true);
            consensusSchedulerMock.Setup(m => m.NewEvent(It.IsIn(int.MaxValue), It.IsAny<BlockMiningEventData>()))
                .Callback(() => ScheduleTriggered = false);
            consensusSchedulerMock.Setup(m => m.CancelCurrentEvent())
                .Callback(() => ScheduleTriggered = false);

            return consensusSchedulerMock.Object;
        }

        private Transaction[] GetGenesisTransactions(int chainId, params Type[] contractTypes)
        {
            return contractTypes.Select(contractType => GetTransactionForDeployment(chainId, contractType)).ToArray();
        }

        private Transaction GetTransactionForDeployment(int chainId, Type contractType)
        {
            var zeroAddress = _smartContractAddressService.GetZeroSmartContractAddress();

            var code = File.ReadAllBytes(contractType.Assembly.Location);
            return new Transaction
            {
                From = zeroAddress,
                To = zeroAddress,
                MethodName = nameof(ISmartContractZero.DeploySmartContract),
                Params = ByteString.CopyFrom(ParamsPacker.Pack(2, code))
            };
        }

        #endregion
    }
}