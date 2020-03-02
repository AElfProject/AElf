using System.Threading.Tasks;
using AElf.Kernel.ChainController.Application;
using AElf.Kernel.SmartContract.Application;
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
                .Singleton<ILocalParallelTransactionExecutingService, LocalTransactionExecutingService>());
            services.AddSingleton(provider =>
            {
                var mockTxCostStrategy = new Mock<ICalculateTxCostStrategy>();
                mockTxCostStrategy.Setup(m => m.GetCostAsync(It.IsAny<IChainContext>(), It.IsAny<int>()))
                    .Returns((IChainContext x, int y) => Task.FromResult(100000L));
                
                return mockTxCostStrategy.Object;
            });
            services.AddSingleton(provider =>
            {
                var mockReadCostStrategy = new Mock<ICalculateReadCostStrategy>();
                mockReadCostStrategy.Setup(m => m.GetCostAsync(It.IsAny<IChainContext>(), It.IsAny<int>()))
                    .Returns((IChainContext x, int y) => Task.FromResult(100000L));
                
                return mockReadCostStrategy.Object;
            });
            services.AddSingleton(provider =>
            {
                var mockWriteCostStrategy = new Mock<ICalculateWriteCostStrategy>();
                mockWriteCostStrategy.Setup(m => m.GetCostAsync(It.IsAny<IChainContext>(), It.IsAny<int>()))
                    .Returns((IChainContext x, int y) => Task.FromResult(100000L));
                
                return mockWriteCostStrategy.Object;
            });
            services.AddSingleton(provider =>
            {
                var mockStoCostStrategy = new Mock<ICalculateStorageCostStrategy>();
                mockStoCostStrategy.Setup(m => m.GetCostAsync(It.IsAny<IChainContext>(), It.IsAny<int>()))
                    .Returns((IChainContext x, int y) => Task.FromResult(100000L));
                
                return mockStoCostStrategy.Object;
            });
            services.AddSingleton(provider =>
            {
                var mockNetCostStrategy = new Mock<ICalculateTrafficCostStrategy>();
                mockNetCostStrategy.Setup(m => m.GetCostAsync(It.IsAny<IChainContext>(), It.IsAny<int>()))
                    .Returns((IChainContext x, int y) => Task.FromResult(100000L));
                
                return mockNetCostStrategy.Object;
            });
        }

    }
}