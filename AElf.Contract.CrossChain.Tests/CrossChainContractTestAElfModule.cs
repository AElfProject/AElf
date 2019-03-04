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
            context.Services.AddTransient<ITransactionResultSettingService, NoBranchTransactionResultService>();
            context.Services.AddTransient<ITransactionResultGettingService, NoBranchTransactionResultService>();
        }
    }
}