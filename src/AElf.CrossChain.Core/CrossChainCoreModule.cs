using AElf.CrossChain.Indexing.Application;
using AElf.CrossChain.Indexing.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.CrossChain
{
    public class CrossChainCoreModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var crossChainConfiguration = context.Services.GetConfiguration()
                    .GetSection(CrossChainConstants.CrossChainExtraDataKey);
            Configure<CrossChainConfigOptions>(crossChainConfiguration);

            context.Services
                .AddSingleton<IBlocksExecutionSucceededLogEventProcessor, CrossChainIndexingDataProposedLogEventProcessor>();
            context.Services.AddSingleton<IIrreversibleBlockStateProvider, IrreversibleBlockStateProvider>();
        }
    }
}