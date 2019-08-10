using AElf.Contracts.TestKet.AEDPoSExtension;
using AElf.Kernel.SmartContract;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contract.Vote
{
    [DependsOn(typeof(ContractTestAEDPoSExtensionModule))]
    public class VoteContractTestAElfModule : ContractTestAEDPoSExtensionModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<VoteContractTestAElfModule>();
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        }
    }
}