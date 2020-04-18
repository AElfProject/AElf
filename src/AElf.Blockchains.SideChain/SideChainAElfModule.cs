using AElf.Blockchains.BasicBaseChain;
using AElf.Blockchains.ContractInitialization;
using AElf.Kernel.Token;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.Modularity;

namespace AElf.Blockchains.SideChain
{
    [DependsOn(
        typeof(BasicBaseChainAElfModule),
        typeof(SideChainContractInitializationAElfModule)
    )]
    public class SideChainAElfModule : AElfModule
    {
        public ILogger<SideChainAElfModule> Logger { get; set; }

        public SideChainAElfModule()
        {
            Logger = NullLogger<SideChainAElfModule>.Instance;
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IPrimaryTokenSymbolProvider, SideChainPrimaryTokenSymbolProvider>();
        }
    }
}