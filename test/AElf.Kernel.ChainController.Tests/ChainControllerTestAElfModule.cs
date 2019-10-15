using System.Collections.Generic;
using AElf.Kernel.ChainController.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.ChainController
{
    [DependsOn(
        typeof(ChainControllerAElfModule),
        typeof(KernelCoreTestAElfModule))]
    public class ChainControllerTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            
            services.AddTransient<ChainCreationService>();
            services.AddSingleton<ISystemTransactionMethodNameListProvider, SystemTransactionMethodNameListProvider>();
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