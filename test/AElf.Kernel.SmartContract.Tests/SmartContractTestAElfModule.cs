using System.Collections.Generic;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Kernel.TransactionPool.Application;
using AElf.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract
{
    [DependsOn(
        typeof(SmartContractAElfModule),
        typeof(KernelCoreTestAElfModule))]
    public class SmartContractTestAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddSingleton<SmartContractRunnerContainer>();
            
            Configure<HostSmartContractBridgeContextOptions>(options =>
            {
                options.ContextVariables[ContextVariableDictionary.NativeSymbolName] = "ELF";
                options.ContextVariables[ContextVariableDictionary.ResourceTokenSymbolList] = "RAM,STO,CPU,NET";
            });
            
            context.Services
                .AddTransient(provider =>
                {
                    var service = new Mock<ISystemTransactionMethodNameListProvider>();
                    service.Setup(m => m.GetSystemTransactionMethodNameList())
                        .Returns(new List<string>
                            {
                                "InitialAElfConsensusContract",
                                "FirstRound",
                                "NextRound",
                                "NextTerm",
                                "UpdateValue",
                                "UpdateTinyBlockInformation"
                            }
                        );

                    return service.Object;
                });
        }
    }
}