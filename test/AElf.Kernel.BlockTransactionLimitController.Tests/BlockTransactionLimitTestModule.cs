using AElf.Contracts.TestKit;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace AElf.Kernel.BlockTransactionLimitController.Tests
{
    [DependsOn(typeof(ContractTestModule),
        typeof(BlockTransactionLimitControllerModule))]
    public class BlockTransactionLimitTestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton(typeof(LogEventListeningService<>));
            context.Services
                .Replace(ServiceDescriptor
                    .Singleton<ILogEventListeningService<IBlockAcceptedLogEventHandler>,
                        OptionalLogEventListeningService<IBlockAcceptedLogEventHandler>>());
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false );
        }
    }
}