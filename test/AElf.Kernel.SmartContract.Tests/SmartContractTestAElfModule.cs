using System.Collections.Generic;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Modularity;
using AElf.Runtime.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract
{
    [DependsOn(
        typeof(SmartContractAElfModule),
        typeof(KernelCoreTestAElfModule),
        typeof(CSharpRuntimeAElfModule))]
    public class SmartContractTestAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddSingleton<SmartContractRunnerContainer>();
            services.AddSingleton<IDefaultContractZeroCodeProvider, UnitTestContractZeroCodeProvider>();
            services.AddSingleton<SmartContractHelper>();
            
            Configure<HostSmartContractBridgeContextOptions>(options =>
            {
                options.ContextVariables[ContextVariableDictionary.NativeSymbolName] = "ELF";
                options.ContextVariables["SymbolListToPayTxFee"] = "WRITE,STO,READ,NET";
            });
        }
    }
}