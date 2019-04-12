using System;
using AElf.Common;
using AElf.Database;
using AElf.Kernel;
using AElf.Kernel.ChainController;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract;
using AElf.Modularity;
using AElf.Runtime.CSharp;
using AElf.Kernel.SmartContract.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;
using NSubstitute.Extensions;
using Org.BouncyCastle.Math.EC;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Runtime.CSharp
{
    [DependsOn(
        typeof(CSharpRuntimeAElfModule),
        typeof(SmartContractTestAElfModule)
    )]
    public class TestCSharpRuntimeAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            
        }
    }
}