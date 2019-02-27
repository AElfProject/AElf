using System;
using AElf.Common;
using AElf.Database;
using AElf.Kernel;
using AElf.Kernel.ChainController;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract;
using AElf.Modularity;
using AElf.Runtime.CSharp;
using AElf.Kernel.SmartContract.Contexts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NSubstitute.Extensions;
using Org.BouncyCastle.Math.EC;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Runtime.CSharp
{
    [DependsOn(
        typeof(ChainControllerAElfModule),
        typeof(SmartContractAElfModule),
        typeof(CSharpRuntimeAElfModule),
        typeof(CoreKernelAElfModule)
    )]
    public class TestCSharpRuntimeAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<TestCSharpRuntimeAElfModule>();

            context.Services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o => o.UseInMemoryDatabase());
            context.Services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());

            Configure<RunnerOptions>(o => new RunnerOptions());
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
        }
    }
}