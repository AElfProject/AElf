using System;
using System.Collections.Generic;
using System.IO;
using AElf.Miner.Tests;
using AElf.Modularity;
using AElf.Runtime.CSharp;
using AElf.SmartContract;
using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Volo.Abp.Modularity;

namespace AElf.Miner.Tests
{
    [DependsOn(typeof(SmartContractAElfModule))]
    public class MockCSharpRuntimeAElfModule : CSharpRuntimeAElfModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            configuration["Runner"] = $@"
            {{
                ""SdkDir"": ""{ContractCodes.TestContractFolder}""
            }}";
            Configure<RunnerOptions>(configuration.GetSection("Runner"));
        }
    }
}