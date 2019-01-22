using AElf.Runtime.CSharp;
using AElf.SmartContract;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.SideChain.Tests
{
    [DependsOn(typeof(SmartContractAElfModule))]
    public class MockCSharpRuntimeAElfModule : CSharpRuntimeAElfModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            configuration["Runner"] = $@"
            {{
                ""SdkDir"": ""../../../../AElf.Runtime.CSharp.Tests.TestContract/bin/Debug/netstandard2.0/""
            }}";
            Configure<RunnerOptions>(configuration.GetSection("Runner"));
        }
    }
}