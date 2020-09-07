using AElf.Modularity;
using AElf.OS.Network;
using Volo.Abp.Modularity;

namespace AElf.OS.Handlers
{
    [DependsOn(
        typeof(NetworkServiceTestModule),
        typeof(OSTestAElfModule))]
    public class AbnormalPeerEventHandlerTestAElfModule : AElfModule
    {

    }
}