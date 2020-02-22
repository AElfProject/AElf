using AElf.Contracts.TestBase;
using AElf.Contracts.TestKit;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace AElf.Contracts.MultiToken
{
    [DependsOn(typeof(ContractTestModule))]
    public class MultiTokenContractTestAElfModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var instance = new TestTokenBalanceTransactionGenerator();
            context.Services.AddSingleton(instance);
            context.Services.AddSingleton<ISystemTransactionGenerator>(instance);
            context.Services.RemoveAll<IPreExecutionPlugin>();
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        }
    }

    [DependsOn(typeof(ContractTestAElfModule))]
    public class MultiTokenContractCrossChainTestAElfModule : ContractTestAElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var instance = new TestTokenBalanceTransactionGenerator();
            context.Services.AddSingleton(instance);
            context.Services.AddSingleton<ISystemTransactionGenerator>(instance);
            context.Services.RemoveAll<IPreExecutionPlugin>();
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
            
            Configure<HostSmartContractBridgeContextOptions>(options =>
            {
                options.ContextVariables[ContextVariableDictionary.NativeSymbolName] = "ELF";
                options.ContextVariables[ContextVariableDictionary.PayTxFeeSymbolList] = "WRITE,STO,READ,NET";
                options.ContextVariables[ContextVariableDictionary.PayRentalSymbolList] = "CPU,RAM,DISK";
            });
        }
    }
}