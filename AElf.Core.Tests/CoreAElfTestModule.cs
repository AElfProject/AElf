using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf
{
    [DependsOn(
        typeof(CoreAElfModule))]
    public class CoreAElfTestModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {

        }
    }
}