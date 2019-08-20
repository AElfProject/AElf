using AElf.Modularity;
using Volo.Abp.Application;
using Volo.Abp.Modularity;

namespace AElf.GraphQL.Application.Core
{
    [DependsOn(typeof(CoreAElfModule), typeof(AbpDddApplicationModule))]
    // ReSharper disable once InconsistentNaming
    public class CoreApplicationGraphQLAppAElfModule : AElfModule
    {
    }
}