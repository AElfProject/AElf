using System.Collections.Generic;
using AElf.Contracts.TestKit;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs1;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs5;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs8;
using AElf.Kernel.TransactionPool.Application;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Economic.TestBase
{
    [DependsOn(typeof(ContractTestModule))]
    public class EconomicContractsTestModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ContractOptions>(o => o.ContractDeploymentAuthorityRequired = false);

            context.Services.AddSingleton<ITransactionExecutor, EconomicTransactionExecutor>();
            context.Services.AddSingleton<ITriggerInformationProvider, AEDPoSTriggerInformationProvider>();
            context.Services.AddSingleton<IBlockValidationService, MockBlockValidationService>();
            context.Services.AddSingleton<IPreExecutionPlugin, FeeChargePreExecutionPlugin>();
            context.Services.AddSingleton<IPreExecutionPlugin, MethodCallingThresholdPreExecutionPlugin>();
            context.Services.AddSingleton<IPreExecutionPlugin, ResourceConsumptionPreExecutionPlugin>();
            context.Services.AddSingleton<IPostExecutionPlugin, ResourceConsumptionPostExecutionPlugin>();
            context.Services.AddSingleton<IRandomHashCacheService, MockRandomHashCacheService>();
            context.Services.AddSingleton<ITransactionInclusivenessProvider, TransactionInclusivenessProvider>();
                        
            context.Services.AddSingleton<ISecretSharingService, SecretSharingService>();
            context.Services.AddSingleton<IInValueCacheService, InValueCacheService>();

            context.Services
                .AddSingleton<ISystemTransactionMethodNameListProvider, SystemTransactionMethodNameListProvider>();
        }
    }
    
    public class MockRandomHashCacheService : IRandomHashCacheService
    {
        public void SetRandomHash(Hash bestChainHash, Hash randomHash)
        {
        }

        public Hash GetRandomHash(Hash bestChainBlockHash)
        {
            return Hash.FromMessage(bestChainBlockHash);
        }

        public void SetGeneratedBlockBestChainHash(Hash blockHash, long blockHeight)
        {
        }

        public Hash GetLatestGeneratedBlockRandomHash()
        {
            return Hash.FromString("LatestGeneratedBlockRandomHash");
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