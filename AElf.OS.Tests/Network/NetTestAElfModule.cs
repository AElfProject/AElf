using AElf.Modularity;
using AElf.TestBase;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace AElf.OS.Tests.Network
{
    [DependsOn(typeof(TestBaseAElfModule), typeof(AbpEventBusModule))]
    public class NetTestAElfModule : AElfModule
    {

    }
}