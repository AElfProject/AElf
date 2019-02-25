using System;
using AElf.Common;
using AElf.Database;
using AElf.Kernel;
using AElf.Kernel.Infrastructure;
using AElf.Modularity;
using AElf.Runtime.CSharp;
using AElf.Kernel.SmartContract.Contexts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Runtime.CSharp.Tests
{
    /*
    [DependsOn(
        typeof(AElf.ChainController.ChainControllerAElfModule),
        typeof(AElf.SmartContract.SmartContractAElfModule),
        typeof(AElf.Runtime.CSharp.CSharpRuntimeAElfModule),
        typeof(AElf.Runtime.CSharp.CSharpRuntimeAElfModule2),
        typeof(CoreKernelAElfModule)
    )]
    public class TestCSharpRuntimeAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<TestCSharpRuntimeAElfModule>();

            context.Services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o => o.UseInMemoryDatabase());
            context.Services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());
        }


        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            
        }
    }
    */
}