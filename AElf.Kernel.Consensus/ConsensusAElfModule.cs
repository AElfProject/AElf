using System.Collections.Generic;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.DPoS;
using AElf.Kernel.Consensus.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Consensus
{
    [DependsOn(typeof(DPoSConsensusAElfModule))]
    public class ConsensusAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IConsensusService, ConsensusService>();

            context.Services.AddScoped<ISmartContractAddressNameProvider, ConsensusSmartContractAddressNameProvider>();

            context.Services.AddScoped<IBlockExtraDataProvider, ConsensusExtraDataProvider>();

            context.Services.AddSingleton<ConsensusControlInformation>();
        }
    }
}