using System;
using AElf.Common;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Database;
using AElf.Kernel;
using AElf.Kernel.Storages;
using AElf.Modularity;
using AElf.Runtime.CSharp;
using AElf.Sdk.CSharp2.Tests;
using AElf.SmartContract.Contexts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Runtime.CSharp2.Tests
{
    [DependsOn(
        typeof(AElf.ChainController.ChainControllerAElfModule),
        typeof(AElf.SmartContract.SmartContractAElfModule),
        typeof(AElf.Runtime.CSharp.CSharpRuntimeAElfModule),
        typeof(AElf.Runtime.CSharp.CSharpRuntimeAElfModule2),
        typeof(KernelAElfModule)
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
            ChainConfig.Instance.ChainId = Hash.LoadByteArray(new byte[] {0x01, 0x02, 0x03}).DumpBase58();
            NodeConfig.Instance.NodeAccount = Address.Generate().GetFormatted();
        }
    }
}