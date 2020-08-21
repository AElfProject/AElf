using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.TransactionPool.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Kernel.Txn.Application;
using AElf.Modularity;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Kernel.TransactionPool
{
    [DependsOn(
        typeof(TransactionPoolAElfModule),
        typeof(KernelCoreTestAElfModule),
        typeof(SmartContractAElfModule)
    )]
    public class TransactionPoolTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<BasicTransactionValidationProvider>();
        }
    }

    [DependsOn(
        typeof(TransactionPoolTestAElfModule),
        typeof(KernelCoreWithChainTestAElfModule)
    )]
    public class TransactionPoolWithChainTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
    
    [DependsOn(
        typeof(TransactionPoolWithChainTestAElfModule)
    )]
    public class TransactionPoolTxHubTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            Configure<TransactionOptions>(o => { o.PoolLimit = 20; });

            context.Services.RemoveAll<ITransactionValidationProvider>();
            context.Services.AddTransient<ITransactionValidationProvider, BasicTransactionValidationProvider>();
        }
    }

    [DependsOn(
        typeof(TransactionPoolTestAElfModule),
        typeof(KernelCoreWithChainTestAElfModule)
    )]
    public class TransactionExecutionValidationModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddSingleton<TransactionExecutionValidationProvider>();
            services.AddSingleton<TransactionMethodValidationProvider>();
            services.AddSingleton<TransactionMockExecutionHelper>();

            services.AddSingleton(provider =>
            {
                var mockService = new Mock<IPlainTransactionExecutingService>();

                mockService.Setup(m =>
                        m.ExecuteAsync(It.IsAny<TransactionExecutingDto>(), It.IsAny<CancellationToken>()))
                    .Returns<TransactionExecutingDto, CancellationToken>((transactionExecutingDto, cancellationToken) =>
                    {
                        var transactionMockExecutionHelper =
                            context.Services.GetRequiredServiceLazy<TransactionMockExecutionHelper>().Value;
                        return Task.FromResult(new List<ExecutionReturnSet>
                        {
                            new ExecutionReturnSet
                            {
                                Status = transactionMockExecutionHelper.GetTransactionResultStatus()
                            }
                        });
                    });

                return mockService.Object;
            });
            
            services.AddSingleton(provider =>
            {
                var mockService = new Mock<ITransactionReadOnlyExecutionService>();

                mockService.Setup(m =>
                        m.IsViewTransactionAsync(It.IsAny<IChainContext>(), It.IsAny<Transaction>()))
                    .Returns<IChainContext, Transaction>((chainContext, transaction) => Task.FromResult(transaction.MethodName == "View"));

                return mockService.Object;
            });
        }
    }
}