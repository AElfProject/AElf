using AElf.ContractTestKit;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace AElf.Kernel.FeatureManagement.Tests;

[DependsOn(typeof(ContractTestModule),
    typeof(FeatureManagementAElfModule),
    typeof(KernelAElfModule))]
public class FeatureManagementTestModule : ContractTestModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton(typeof(LogEventProcessingService<>));
        context.Services
            .Replace(ServiceDescriptor
                .Singleton<ILogEventProcessingService<IBlockAcceptedLogEventProcessor>,
                    OptionalLogEventProcessingService<IBlockAcceptedLogEventProcessor>>());
        Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
    }
}