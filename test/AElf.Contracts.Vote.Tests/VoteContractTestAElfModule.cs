using AElf.ContractTestKit;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Vote
{
    [DependsOn(typeof(ContractTestModule))]
    public class VoteContractTestAElfModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.RemoveAll<IPreExecutionPlugin>();
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false );
            context.Services.AddSingleton<IResetBlockTimeProvider, ResetBlockTimeProvider>();
        }
    }
}