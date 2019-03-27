using System;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Consensus.DPoS;
using AElf.Modularity;
using AElf.OS;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Blockchains.BasicBaseChain
{
    [DependsOn(
        typeof(DPoSConsensusAElfModule),
        typeof(KernelAElfModule),
        typeof(OSAElfModule)
    )]
    public class BasicBaseChainAElfModule : AElfModule<BasicBaseChainAElfModule>
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var config = context.Services.GetConfiguration();
            Configure<ChainOptions>(option =>
            {
                option.ChainId = ChainHelpers.ConvertBase58ToChainId(config["ChainId"]);
            });
        }
    }
}