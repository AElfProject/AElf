using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Account.Application;
using AElf.Kernel.ChainController;
using AElf.Kernel.Consensus;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContractExecution;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Modularity;

namespace AElf.Kernel
{
    [DependsOn(
         typeof(KernelAElfModule),
         typeof(ConsensusAElfModule),
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

            var transactionList = new List<Transaction>()
            {
                new Transaction
                {
                    From = Address.Generate(),
                    To = Address.Generate(),
                    MethodName = ConsensusConsts.GenerateConsensusTransactions
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
                mockService.Setup(m=>m.GenerateSystemTransactions(It.IsAny<Address>(), It.IsAny<long>(), It.IsAny<Hash>()))
                    .Returns(transactionList);

                return mockService.Object;
            });

            services.AddTransient(o =>
            {
                var mockService = new Mock<IBlockExecutingService>();
                mockService.Setup(m=>m.ExecuteBlockAsync(It.IsAny<BlockHeader>(), It.IsAny<IEnumerable<Transaction>>(), It.IsAny<IEnumerable<Transaction>>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(new Block()
                    {
                        Body = new BlockBody()
                        {
                            TransactionList = { transactionList }
                        },
                        Header = new BlockHeader(),
                        Height = 10
                    }));
                return mockService.Object;
            });
            
            services.AddTransient<IConsensusService, ConsensusService>();
            
            services.AddTransient<ISystemTransactionGenerator, ConsensusTransactionGenerator>();
        }
    }
}