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
        typeof(KernelCoreTestAElfModule),
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
            services.AddTransient(builder =>
            {
                var acc = new Mock<IAccountService>();

                return acc.Object;
            });

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
                    m.GenerateTransactions(It.IsAny<Address>(), It.IsAny<long>(), It.IsAny<Hash>(),
                        ref transactionList));

                return mockService.Object;
            });

            services.AddTransient(o =>
            {
                var mockService = new Mock<ISystemTransactionGenerationService>();
                mockService.Setup(m =>
                        m.GenerateSystemTransactions(It.IsAny<Address>(), It.IsAny<long>(), It.IsAny<Hash>()))
                    .Returns(transactionList);

                return mockService.Object;
            });

            services.AddTransient<IBlockExecutingService, TestBlockExecutingService>();


            //For BlockExtraDataService testing.
            services.AddTransient(
                builder =>
                {
                    var dataProvider = new Mock<IBlockExtraDataProvider>();

                    ByteString bs = ByteString.CopyFrom(BitConverter.GetBytes(long.MaxValue - 1));

                    dataProvider.Setup(m => m.GetExtraDataForFillingBlockHeaderAsync(It.IsAny<BlockHeader>()))
                        .Returns(Task.FromResult(bs));

                    return dataProvider.Object;
                });


//            services.AddTransient<IConsensusService, ConsensusService>();
//            
//            services.AddTransient<ISystemTransactionGenerator, ConsensusTransactionGenerator>();
        }
    }
}