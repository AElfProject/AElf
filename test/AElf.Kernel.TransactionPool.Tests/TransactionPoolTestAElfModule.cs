using System.Threading.Tasks;
using AElf.Kernel.SmartContract;
using AElf.Kernel.TransactionPool.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Modularity;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
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
            context.Services.AddSingleton<TxHub>();
            Configure<TransactionOptions>(o => { o.PoolLimit = 5120; });
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
            var services = context.Services;

            services.AddSingleton(provider =>
            {
                var mockService = new Mock<ITransactionValidationService>();
                mockService.Setup(m => m.ValidateTransactionWhileCollectingAsync(It.IsAny<Transaction>()))
                    .Returns(Task.FromResult(true));

                return mockService.Object;
            });
        }
    }
}