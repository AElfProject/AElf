using System.Collections.Generic;
using AElf.Contracts.TestKit;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs1;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs8;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contract.TestContract
{
    [DependsOn(typeof(ContractTestModule))]
    public class TestContractAElfModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
        }
    }

    [DependsOn(
        typeof(ContractTestModule),
        typeof(ExecutionPluginForAcs1Module),
        typeof(ExecutionPluginForAcs8Module))]
    public class TestFeesContractAElfModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);
            context.Services.AddSingleton<ISystemTransactionMethodNameListProvider, SystemTransactionMethodNameListProvider>();
        } 
    }
    
    public class SystemTransactionMethodNameListProvider : ISystemTransactionMethodNameListProvider, ITransientDependency
    {
        public List<string> GetSystemTransactionMethodNameList()
        {
            return new List<string>
            {
                "InitialAElfConsensusContract",
                "FirstRound",
                "NextRound",
                "NextTerm",
                "UpdateValue",
                "UpdateTinyBlockInformation",
                "ClaimTransactionFees",
                "DonateResourceToken",
                "RecordCrossChainData",
                
                //acs5 check tx
                "CheckThreshold",
                //acs8 check tx
                "CheckResourceToken",
                "ChargeResourceToken",
                //genesis deploy
                "DeploySmartContract",
                "DeploySystemSmartContract"
            };
        }
    }
}