using AElf.Modularity;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.TestBase
{
    [DependsOn(
        typeof(AbpTestBaseModule))]
    public class TestBaseAElfModule : AElfModule
    {
    }
}