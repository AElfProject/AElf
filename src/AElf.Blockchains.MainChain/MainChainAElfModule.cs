using AElf.Blockchains.BasicBaseChain;
using AElf.Kernel.SmartContract.Application;
using AElf.Database;
using AElf.Kernel.Infrastructure;
using AElf.Modularity;
using AElf.OS.Node.Application;
using Microsoft.Extensions.Configuration;
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
            var services = context.Services;
            services.AddTransient<IContractDeploymentListProvider, MainChainContractDeploymentListProvider>();
            services.AddTransient<IGenesisSmartContractDtoProvider, MainChainGenesisSmartContractDtoProvider>();

            var config = context.Services.GetConfiguration();

            if (config.GetConnectionString("BlockchainDb") == "InMemory")
            {
                services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(p => p.UseInMemoryDatabase());
            }
            
            if (config.GetConnectionString("StateDb") == "InMemory")
            {
                services.AddKeyValueDbContext<StateKeyValueDbContext>(p => p.UseInMemoryDatabase());
            }
        }
    }
}