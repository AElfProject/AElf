using AElf.Common;
using AElf.Kernel;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.Consensus.DPoS
{
    // ReSharper disable once InconsistentNaming
    public class DPoSConsensusModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IConsensusService, ConsensusService>();
            context.Services.AddSingleton<IConsensusInformationGenerationService, DPoSInformationGenerationService>();
            context.Services.AddSingleton<IConsensusObserver, DPoSObserver>();
        }

        public override void OnPostApplicationInitialization(ApplicationInitializationContext context)
        {
            var chainOptions = context.ServiceProvider.GetService<IOptions<ChainOptions>>().Value;
            var myService = context.ServiceProvider.GetService<IConsensusService>();
            AsyncHelper.RunSync(() => myService.TriggerConsensusAsync(chainOptions.ChainId.ConvertBase58ToChainId()));
        }
    }
}