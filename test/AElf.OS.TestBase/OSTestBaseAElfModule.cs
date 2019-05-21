using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using AElf.Runtime.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.OS
{
    [DependsOn(
        typeof(CoreOSAElfModule),
        typeof(AEDPoSAElfModule),
        typeof(CSharpRuntimeAElfModule),
        typeof(KernelTestAElfModule)
    )]
    public class OSTestBaseAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<OSTestHelper>();
            //context.Services.AddSingleton<ISmartContractExecutiveService, TestingSmartContractExecutiveService>();
        }
    }
}