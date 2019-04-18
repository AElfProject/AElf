using System;
using AElf.Contracts.TestKit;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Xunit;

namespace AElf.Contracts.Proposal
{
    [DependsOn(typeof(ContractTestModule))]
    public class ProposalContractTestAElfModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<ProposalContractTestAElfModule>();
        }
    }
}