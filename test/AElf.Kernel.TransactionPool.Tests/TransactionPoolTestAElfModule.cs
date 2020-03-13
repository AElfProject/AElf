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

    // [DependsOn(
    //     typeof(TransactionPoolWithChainTestAElfModule)
    // )]
    // public class TransactionPoolValidationTestAElfModule : AElfModule
    // {
    //     public override void ConfigureServices(ServiceConfigurationContext context)
    //     {
    //         var services = context.Services;
    //
    //         services.AddSingleton<TransactionFromAddressBalanceValidationProvider>();
    //         services.AddSingleton(provider =>
    //         {
    //             var service = new Mock<IPrimaryTokenSymbolProvider>();
    //
    //             return service.Object;
    //         });
    //         
    //         services.AddSingleton(provider =>
    //         {
    //             var service = new Mock<ITransactionFeeExemptionService>();
    //             service.Setup(m => m.IsFree(It.Is<Transaction>(tx => tx.MethodName == "SystemMethod")))
    //                 .Returns(true);
    //             service.Setup(m => m.IsFree(It.Is<Transaction>(m => m.MethodName != "SystemMethod")))
    //                 .Returns(false);
    //
    //             return service.Object;
    //         });
    //
    //         services.AddSingleton<TransactionMethodNameValidationProvider>();
    //         services.AddSingleton<NotAllowEnterTxHubValidationProvider>();
    //     }
    // }
}