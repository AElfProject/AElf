using AElf.Blockchains.BasicBaseChain;
using AElf.EconomicSystem;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.ContractsInitialization;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.Modularity;

namespace AElf.Blockchains.MainChain
{
    [DependsOn(
        typeof(BasicBaseChainAElfModule)
    )]
    public class MainChainAElfModule : AElfModule
    {
        public ILogger<MainChainAElfModule> Logger { get; set; }
        
        public MainChainAElfModule()
        {
            Logger = NullLogger<MainChainAElfModule>.Instance;
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<IContractInitializationProvider, AEDPoSContractInitializationProvider>();
            context.Services.AddTransient<IContractInitializationProvider, ProfitContractInitializationProvider>();
            
            context.Services.AddSingleton<IContractDeploymentListProvider, MainChainContractDeploymentListProvider>();
        }
    }
}