using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Account.Application;
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
using AElf.Modularity;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

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
                mockService.Setup(m => m.GetAddressByContractNameAsync(It.IsAny<IChainContext>(), It.IsAny<string>()))
                    .Returns(Task.FromResult(default(Address)));
                return mockService.Object;
            });
        }
    }

    [DependsOn(typeof(KernelWithChainTestAElfModule), typeof(CoreConsensusAElfModule))]
    public class KernelWithConsensusStaffTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddTransient(builder =>
            {
                var mockConsensusScheduler = new Mock<IConsensusScheduler>();
                return mockConsensusScheduler.Object;
            });

            services.AddTransient(builder =>
            {
                var mockTriggerInformationProvider = new Mock<ITriggerInformationProvider>();
                return mockTriggerInformationProvider.Object;
            });

            services.AddTransient(provider =>
            {
                var mockService = new Mock<IConsensusExtraDataExtractor>();
                mockService.Setup(m => m.ExtractConsensusExtraData(It.Is<BlockHeader>(o => o.Height == 9)))
                    .Returns(ByteString.Empty);
                mockService.Setup(m => m.ExtractConsensusExtraData(It.Is<BlockHeader>(o => o.Height != 9)))
                    .Returns(ByteString.CopyFromUtf8("test"));
                return mockService.Object;
            });
            
            services.AddTransient(provider =>
            {
                var mockService = new Mock<ISmartContractAddressService>();
                mockService.Setup(m => m.GetAddressByContractNameAsync(It.IsAny<IChainContext>(), It.IsAny<string>()))
                    .Returns(Task.FromResult(default(Address)));
                return mockService.Object;
            });
        }
    }
}
