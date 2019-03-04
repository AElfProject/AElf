using System.Collections.Generic;
using AElf.Contracts.TestBase;
using AElf.Kernel.Blockchain.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contract.CrossChain.Tests
{
    [DependsOn(
        typeof(ContractTestAElfModule)
    )]
    public class CrossChainContractTestAElfModule : AElfModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<ITransactionResultService, NoBranchTransactionResultService>();
            context.Services.AddTransient<ITransactionResultQueryService, NoBranchTransactionResultService>();
        }
        public override void PostConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.RemoveAll(x =>
                (x.ServiceType == typeof(ITransactionResultService) ||
                 x.ServiceType == typeof(ITransactionResultQueryService)) &&
                x.ImplementationType != typeof(NoBranchTransactionResultService));
        }
    }
}