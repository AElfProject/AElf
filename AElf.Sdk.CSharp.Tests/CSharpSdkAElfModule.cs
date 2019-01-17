using AElf.Database;
using AElf.Kernel;
using AElf.Kernel.Storages;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Sdk.CSharp.Tests
{
    [DependsOn(
        typeof(AElf.ChainController.ChainControllerAElfModule),
        typeof(AElf.SmartContract.SmartContractAElfModule),
        typeof(AElf.Runtime.CSharp.CSharpRuntimeAElfModule),
        typeof(KernelAElfModule)
    )]
    public class TestCSharpSdkAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<TestCSharpSdkAElfModule>();
            
            context.Services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o => o.UseInMemoryDatabase());
            context.Services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());
        }
    }
}