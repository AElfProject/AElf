using AElf.Kernel.Consensus.AEPoW.Application;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.Consensus.Scheduler.RxNet;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Modularity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Consensus.AEPoW
{
    [DependsOn(
        typeof(RxNetSchedulerAElfModule),
        typeof(CoreConsensusAElfModule)
    )]
    // ReSharper disable once InconsistentNaming
    public class AEPoWAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services
                .AddSingleton<IContractInitializationProvider, DefaultConsensusContractInitializationProvider>();
            context.Services.AddTransient<IFillBlockAfterExecutionService, AEPoWFillBlockAfterExecutionService>();

            var configuration = context.Services.GetConfiguration();
            Configure<AEPoWOptions>(option =>
            {
                var aeDPoSOptions = configuration.GetSection("AEDPoS");
                aeDPoSOptions.Bind(option);
            });
        }
    }
}