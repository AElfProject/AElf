using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Blockchains.ContractInitialization
{
    public class SideChainContractInitializationAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddTransient<IContractInitializationProvider, ProfitContractInitializationProvider>();
            services.AddTransient<IContractInitializationProvider, TokenHolderContractInitializationProvider>();
            services.AddTransient<IContractInitializationProvider, SideChainAEDPosContractInitializationProvider>();
            services.AddTransient<IContractInitializationProvider, SideChainTokenContractInitializationProvider>();
            services.AddTransient<IContractInitializationProvider, SideChainParliamentContractInitializationProvider>();
            services.AddTransient<IContractInitializationProvider, ReferendumContractInitializationProvider>();
            services.AddTransient<IContractInitializationProvider, AssociationContractInitializationProvider>();
            services.AddTransient<IContractInitializationProvider, SideChainCrossChainContractInitializationProvider>();
            services.AddTransient<IContractInitializationProvider, ConfigurationContractInitializationProvider>();
        }
    }
}