using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Account.Infrastructure;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.ChainController;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool;
using AElf.Kernel.TransactionPool.Application;
using AElf.Modularity;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NSubstitute;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;
using BlockValidationProvider = AElf.Kernel.Miner.Application.BlockValidationProvider;

namespace AElf.Kernel
{
    [DependsOn(
        typeof(KernelAElfModule),
        typeof(SmartContractTestAElfModule),
        typeof(SmartContractExecutionTestAElfModule),
        typeof(TransactionPoolTestAElfModule),
        typeof(ChainControllerTestAElfModule)
    )]
    public class KernelTestAElfModule : AElfModule
    {
    }

    [DependsOn(
        typeof(KernelTestAElfModule),
        typeof(KernelCoreWithChainTestAElfModule))]
    public class KernelWithChainTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            var transactionList = new List<Transaction>
            {
                new Transaction
                {
                    From = SampleAddress.AddressList[0],
                    To = SampleAddress.AddressList[1],
                    MethodName = "GenerateConsensusTransactions"
                }
            };
            services.AddTransient(o =>
            {
                var mockService = new Mock<ISystemTransactionGenerator>();
                mockService.Setup(m =>
                    m.GenerateTransactionsAsync(It.IsAny<Address>(), It.IsAny<long>(), It.IsAny<Hash>()));

                return mockService.Object;
            });

            services.AddTransient(o =>
            {
                var mockService = new Mock<ISystemTransactionGenerationService>();
                mockService.Setup(m =>
                        m.GenerateSystemTransactionsAsync(It.IsAny<Address>(), It.IsAny<long>(), It.IsAny<Hash>()))
                    .Returns(Task.FromResult(transactionList));

                return mockService.Object;
            });

            services.AddTransient<IBlockExecutingService, TestBlockExecutingService>();


            //For BlockExtraDataService testing.
            services.AddTransient(
                builder =>
                {
                    var dataProvider = new Mock<IBlockExtraDataProvider>();

                    ByteString bs = ByteString.CopyFrom(BitConverter.GetBytes(long.MaxValue - 1));

                    dataProvider.Setup(m => m.GetBlockHeaderExtraDataAsync(It.IsAny<BlockHeader>()))
                        .Returns(Task.FromResult(bs));

                    dataProvider.Setup(d => d.BlockHeaderExtraDataKey).Returns("TestExtraDataKey");

                    return dataProvider.Object;
                });
            services.AddTransient(provider =>
            {
                var mockService = new Mock<ISmartContractAddressService>();
                return mockService.Object;
            });

            services.AddTransient<BlockValidationProvider>();
        }
    }

    [DependsOn(typeof(KernelWithChainTestAElfModule))]
    public class KernelMiningTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            context.Services.AddTransient<IAccountService, AccountService>();

            services.AddSingleton(provider =>
            {
                var mockService = new Mock<ITransactionValidationService>();
                mockService.Setup(m =>
                        m.ValidateTransactionWhileCollectingAsync(It.IsAny<IChainContext>(), It.IsAny<Transaction>()))
                    .Returns(Task.FromResult(true));

                return mockService.Object;
            });

            services.AddTransient(provider =>
            {
                var mockService = new Mock<ISmartContractAddressService>();
                return mockService.Object;
            });
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var kernelTestHelper = context.ServiceProvider.GetRequiredService<KernelTestHelper>();
            var ecKeyPairProvider = context.ServiceProvider.GetService<IAElfAsymmetricCipherKeyPairProvider>();
            ecKeyPairProvider.SetKeyPair(kernelTestHelper.KeyPair);
        }
    }

    [DependsOn(typeof(KernelMiningTestAElfModule), typeof(CoreConsensusAElfModule))]
    public class KernelConsensusRequestMiningTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            
            services.AddTransient(provider =>
            {
                var mockService = new Mock<ISmartContractAddressService>();
                return mockService.Object;
            });

            var consensusRequestMiningTestContext = new KernelConsensusRequestMiningTestContext();
            
            services.AddSingleton(builder =>
            {
                var mockConsensusService = new Mock<IConsensusService>();

                consensusRequestMiningTestContext.MockConsensusService = mockConsensusService;

                return mockConsensusService.Object;
            });

            services.AddSingleton<KernelConsensusRequestMiningTestContext>(consensusRequestMiningTestContext);

            services.AddTransient(provider =>
            {
                var mockService = new Mock<IConsensusExtraDataExtractor>();
                return mockService.Object;
            });
        }
    }

    public class KernelConsensusRequestMiningTestContext
    {
        public Mock<IConsensusService> MockConsensusService { get; set; }
    }
}
