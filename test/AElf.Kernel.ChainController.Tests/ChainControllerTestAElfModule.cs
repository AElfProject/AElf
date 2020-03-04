using System.Threading.Tasks;
using AElf.Kernel.ChainController.Application;
using AElf.Kernel.FeeCalculation;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.Token;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Kernel.ChainController
{
    [DependsOn(
        typeof(ChainControllerAElfModule),
        typeof(KernelCoreTestAElfModule)
    )]
    public class ChainControllerTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddTransient<ChainCreationService>();
            services.AddSingleton<IPrimaryTokenSymbolProvider, DefaultPrimaryTokenSymbolProvider>();
            context.Services.Replace(ServiceDescriptor
                .Singleton<ITransactionExecutingService, PlainTransactionExecutingService>());
            services.AddSingleton(provider =>
            {
                var txTokenFeeProvider = new Mock<IPrimaryTokenFeeProvider>();
                txTokenFeeProvider.Setup(m => m.CalculateTokenFeeAsync(It.IsAny<ITransactionContext>(), It.IsAny<IChainContext>()))
                    .Returns((ITransactionContext x, IChainContext y) => Task.FromResult(100000L));
                return txTokenFeeProvider.Object;
            });
        }

    }
}