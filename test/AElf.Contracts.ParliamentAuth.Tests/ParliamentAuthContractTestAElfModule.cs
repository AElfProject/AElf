using System;
using AElf.Contracts.TestKit;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Xunit;

namespace AElf.Contracts.ParliamentAuth
{
    [DependsOn(typeof(ContractTestModule))]
    public class ParliamentAuthContractTestAElfModule : ContractTestModule
    {
        public override  void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<ParliamentAuthContractTestAElfModule>();
        }
    }
}