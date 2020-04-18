using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Blockchains.ContractInitialization
{
    public class MainChainContractInitializationAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            services.AddTransient<IContractInitializationProvider, VoteContractInitializationProvider>();
            services.AddTransient<IContractInitializationProvider, ProfitContractInitializationProvider>();
            services.AddTransient<IContractInitializationProvider, ElectionContractInitializationProvider>();
            services.AddTransient<IContractInitializationProvider, TreasuryContractInitializationProvider>();
            services.AddTransient<IContractInitializationProvider, MainChainTokenContractInitializationProvider>();
            services.AddTransient<IContractInitializationProvider, MainChainParliamentContractInitializationProvider>();
            services.AddTransient<IContractInitializationProvider, AssociationContractInitializationProvider>();
            services.AddTransient<IContractInitializationProvider, MainChainCrossChainContractInitializationProvider>();
            services.AddTransient<IContractInitializationProvider, ConfigurationContractInitializationProvider>();
            services.AddTransient<IContractInitializationProvider, MainChainAEDPosContractInitializationProvider>();
            services.AddTransient<IContractInitializationProvider, TokenConverterContractInitializationProvider>();
            services.AddTransient<IContractInitializationProvider, TokenHolderContractInitializationProvider>();
            services.AddTransient<IContractInitializationProvider, EconomicContractInitializationProvider>();
            services.AddTransient<IContractInitializationProvider, ReferendumContractInitializationProvider>();
        }
    }
}