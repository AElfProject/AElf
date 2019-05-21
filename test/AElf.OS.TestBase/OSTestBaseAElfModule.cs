using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Sdk;
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
            context.Services.AddSingleton<ISmartContractExecutiveService, TestingSmartContractExecutiveService>();
        }
    }

    public class TestingSmartContractExecutiveService : SmartContractExecutiveService
    {
        public TestingSmartContractExecutiveService(ISmartContractRunnerContainer smartContractRunnerContainer,
            IStateProviderFactory stateProviderFactory,
            IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider,
            IHostSmartContractBridgeContextService hostSmartContractBridgeContextService) : base(
            smartContractRunnerContainer, stateProviderFactory, defaultContractZeroCodeProvider,
            hostSmartContractBridgeContextService)
        {
        }

        public override Task PutExecutiveAsync(Address address, IExecutive executive)
        {
            if (_executivePools.TryGetValue(address, out var pool))
            {
                pool.Add(executive);
            }

            return Task.CompletedTask;
        }
    }
}