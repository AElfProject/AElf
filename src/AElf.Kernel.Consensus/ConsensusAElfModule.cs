using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.Infrastructure;
using AElf.Kernel.SmartContractExecution;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Consensus
{
    [DependsOn(typeof(SmartContractExecutionAElfModule))]
    public class ConsensusAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IConsensusService, ConsensusService>();
            context.Services.AddSingleton<ConsensusControlInformation>();
        }
    }
}