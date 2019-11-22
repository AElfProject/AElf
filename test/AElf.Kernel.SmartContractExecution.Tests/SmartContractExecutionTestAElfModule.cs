using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContractExecution
{
    [DependsOn(
        typeof(SmartContractExecutionAElfModule),
        typeof(KernelCoreTestAElfModule)
    )]
    public class SmartContractExecutionTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.Replace(ServiceDescriptor.Singleton<ILocalParallelTransactionExecutingService, LocalTransactionExecutingService>());
            context.Services.AddTransient(provider =>
            {
                var mockService = new Mock<IDeployedContractAddressService>();
                mockService.Setup(m => m.InitAsync());
                return mockService.Object;
            });
        }
    }
}