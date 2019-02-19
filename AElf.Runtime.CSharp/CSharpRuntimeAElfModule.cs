using System;
using System.Collections.Generic;
using System.IO;
using AElf.Modularity;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Volo.Abp.Modularity;

namespace AElf.Runtime.CSharp
{
    [DependsOn(typeof(SmartContractAElfModule))]
    public class CSharpRuntimeAElfModule : AElfModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            Configure<RunnerOptions>(configuration.GetSection("Runner"));
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<ISmartContractRunner, SmartContractRunner>(provider =>
            {
                var option = provider.GetService<IOptions<RunnerOptions>>();
                return new SmartContractRunner(option.Value.SdkDir, option.Value.BlackList, option.Value.WhiteList);
            });
            context.Services.AddSingleton<ISmartContractRunner, SmartContractRunnerForCategoryOne>(provider =>
            {
                var option = provider.GetService<IOptions<RunnerOptions>>();
                return new SmartContractRunnerForCategoryOne(option.Value.SdkDir, option.Value.BlackList,
                    option.Value.WhiteList);
            });
        }
    }
}