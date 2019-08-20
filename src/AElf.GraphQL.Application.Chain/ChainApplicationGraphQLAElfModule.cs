using AElf.GraphQL.Application.Core;
using AElf.Kernel;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.GraphQL.Application.Chain
{
    [DependsOn(typeof(CoreKernelAElfModule), typeof(CoreApplicationGraphQLAppAElfModule))]
    // ReSharper disable once InconsistentNaming
    public class ChainApplicationGraphQLAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IChainStatusRepository, ChainStatusRepository>();
        }
    }
}