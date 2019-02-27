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
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.DPoS.Application;
using AElf.Kernel.Consensus.Infrastructure;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Node.Application;
using AElf.Kernel.SmartContractExecution.Application;
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
    public class ConsensusTester
    {
        private readonly int _chainId;
        
        private readonly IConsensusService _consensusService;
        private readonly IConsensusInformationGenerationService _consensusInformationGenerationService;
        private readonly IAccountService _accountService;//Should mock one.
        private readonly ITransactionExecutingService _transactionExecutingService;
        private readonly IConsensusScheduler _consensusScheduler;
        private readonly IBlockchainService _blockchainService;
        
        private readonly IBlockchainNodeContextService _blockchainNodeContextService;
        private readonly IBlockGenerationService _blockGenerationService;
        private ISystemTransactionGenerationService _systemTransactionGenerationService;
        private readonly IBlockExecutingService _blockExecutingService;
        private readonly IBlockchainExecutingService _blockchainExecutingService;
        private readonly IChainManager _chainManager;
        private readonly ITransactionResultManager _transactionResultManager;
        private readonly IBlockValidationService _blockValidationService;


        public Chain Chain => AsyncHelper.RunSync(GetChainAsync);

        public ECKeyPair CallOwnerKeyPair { get; set; }

        public List<Address> DeployedContractsAddresses { get; set; }

        public ConsensusTester(int chainId, ECKeyPair callOwnerKeyPair, List<ECKeyPair> initialMinersKeyPairs, bool isBootMiner = false)
        {
            _chainId = (chainId == 0) ? ChainHelpers.ConvertBase58ToChainId("AELF") : chainId;

            CallOwnerKeyPair = callOwnerKeyPair ?? CryptoHelpers.GenerateKeyPair();
            
            var application =
                AbpApplicationFactory.Create<DPoSConsensusTestAElfModule>(options => { options.UseAutofac(); });
            application.Initialize();

            _transactionExecutingService = application.ServiceProvider.GetService<ITransactionExecutingService>();
            _consensusScheduler = application.ServiceProvider.GetService<IConsensusScheduler>();
            _blockchainService = application.ServiceProvider.GetService<IBlockchainService>();
            _blockchainNodeContextService = application.ServiceProvider.GetService<IBlockchainNodeContextService>();
            _blockExecutingService = application.ServiceProvider.GetService<IBlockExecutingService>();
            //_blockGenerationService = application.ServiceProvider.GetService<IBlockGenerationService>();
            //_blockValidationService = application.ServiceProvider.GetService<IBlockValidationService>();
            //_blockchainExecutingService = application.ServiceProvider.GetService<IBlockchainExecutingService>();

            // Mock dpos options.
            var consensusOptionsMock = new Mock<IOptionsSnapshot<DPoSOptions>>();
            consensusOptionsMock.Setup(m => m.Value).Returns(new DPoSOptions
            {
                InitialMiners = initialMinersKeyPairs.Select(p => p.PublicKey.ToHex()).ToList(),
                IsBootMiner = isBootMiner,
                MiningInterval = DPoSConsensusConsts.MiningInterval
            });
            var consensusOptions = consensusOptionsMock.Object;
            
            // Mock AccountService.
            var mockAccountService = new Mock<IAccountService>();
            mockAccountService.Setup(s => s.GetPublicKeyAsync()).ReturnsAsync(CallOwnerKeyPair.PublicKey);
            mockAccountService.Setup(s => s.GetAccountAsync())
                .ReturnsAsync(Address.FromPublicKey(CallOwnerKeyPair.PublicKey));
            _accountService = mockAccountService.Object;

            var consensusControlInformation = new ConsensusControlInformation();

            _consensusInformationGenerationService =
                new DPoSInformationGenerationService(consensusOptions, _accountService, consensusControlInformation);

            _consensusService = new ConsensusService(_consensusInformationGenerationService, _accountService,
                _transactionExecutingService, _consensusScheduler, _blockchainService, consensusControlInformation);

            _systemTransactionGenerationService = new SystemTransactionGenerationService(
                new List<ISystemTransactionGenerator> {new ConsensusTransactionGenerator(_consensusService)});
            
            // Initial a chain.
            var transactions = GetGenesisTransactions(_chainId, typeof(BasicContractZero), typeof(ConsensusContract));
            var dto = new OsBlockchainNodeContextStartDto
            {
                BlockchainNodeContextStartDto = new BlockchainNodeContextStartDto
                {
                    ChainId = _chainId,
                    Transactions = transactions
                }
            };

            AsyncHelper.RunSync(() => _blockchainNodeContextService.StartAsync(dto.BlockchainNodeContextStartDto));
//            _chainManager = application.ServiceProvider.GetService<IChainManager>();
//            _transactionResultManager = application.ServiceProvider.GetService<ITransactionResultManager>();
        }
        
        private Transaction[] GetGenesisTransactions(int chainId, params Type[] contractTypes)
        {
            return contractTypes.Select(contractType => GetTransactionForDeployment(chainId, contractType)).ToArray();
        }
        
        private Transaction GetTransactionForDeployment(int chainId, Type contractType)
        {
            var zeroAddress = Address.BuildContractAddress(chainId, 0);

            var code = File.ReadAllBytes(contractType.Assembly.Location);
            return new Transaction
            {
                From = zeroAddress,
                To = zeroAddress,
                MethodName = nameof(ISmartContractZero.DeploySmartContract),
                Params = ByteString.CopyFrom(ParamsPacker.Pack(2, code))
            };
        }

        public async Task TriggerConsensusAsync()
        {
            await _consensusService.TriggerConsensusAsync(_chainId);
        }
        
        public async Task<bool> ValidateConsensusAsync(byte[] consensusInformation)
        {
            return await _consensusService.ValidateConsensusAsync(_chainId, Chain.BestChainHash, Chain.BestChainHeight,
                consensusInformation);
        }

        public async Task<byte[]> GetNewConsensusInformationAsync()
        {
            return await _consensusService.GetNewConsensusInformationAsync(_chainId);
        }

        public async Task<IEnumerable<Transaction>> GenerateConsensusTransactionsAsync()
        {
            return await _consensusService.GenerateConsensusTransactionsAsync(_chainId, Chain.BestChainHeight,
                Chain.BestChainHash.Value.Take(4).ToArray());
        }

        public async Task<Chain> GetChainAsync()
        {
            return await _blockchainService.GetChainAsync(_chainId);
        }
    }
}