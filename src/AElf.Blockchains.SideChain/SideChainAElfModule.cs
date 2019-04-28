using AElf.Blockchains.BasicBaseChain;
using AElf.CrossChain;
using AElf.Kernel;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.Token;
using AElf.Modularity;
using AElf.OS.Node.Application;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.Modularity;

namespace AElf.Blockchains.SideChain
{
    [DependsOn(
        typeof(BasicBaseChainAElfModule)
    )]
    public class SideChainAElfModule : AElfModule<SideChainAElfModule>
    {
        public ILogger<SideChainAElfModule> Logger { get; set; }

        public SideChainAElfModule()
        {
            Logger = NullLogger<SideChainAElfModule>.Instance;
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            base.ConfigureServices(context);
            context.Services.AddTransient<IGenesisSmartContractDtoProvider, GenesisSmartContractDtoProvider>();
        }
    }
}