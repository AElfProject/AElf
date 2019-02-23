using System;
using System.IO;
using AElf.Common;
using AElf.Contracts.Genesis;
using AElf.Contracts.TestBase;
using AElf.Kernel;
using AElf.Kernel.Node;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution;
using AElf.Modularity;
using AElf.OS;
using AElf.Runtime.CSharp;
using AElf.RuntimeSetup;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Consensus.Tests
{
    [DependsOn(
        typeof(RuntimeSetupAElfModule),
        typeof(AbpAutofacModule),
        typeof(SmartContractExecutionAElfModule),
        typeof(CSharpRuntimeAElfModule2),
        typeof(KernelAElfModule),
        typeof(ContractTestAElfModule),
        typeof(NodeAElfModule))]
    public class ConsensusContractTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<ConsensusContractTestAElfModule>();
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var contractZero = typeof(BasicContractZero);
            var code = File.ReadAllBytes(contractZero.Assembly.Location);
            var provider = context.ServiceProvider.GetService<IDefaultContractZeroCodeProvider>();
            provider.DefaultContractZeroRegistration = new SmartContractRegistration
            {
                Category = 2,
                Code = ByteString.CopyFrom(code),
                CodeHash = Hash.FromRawBytes(code)
            };
        }
    }
}