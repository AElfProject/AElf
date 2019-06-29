using AElf.Modularity;
using AElf.OS.Network.Grpc;
using Volo.Abp.Modularity;

namespace AElf.OS.Network
{
    [DependsOn(typeof(OSCoreTestAElfModule), typeof(GrpcNetworkModule))]
    public class GrpcBasicNetworkTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<NetworkOptions>(o =>
            {
                o.ListeningPort = 2000;
                o.MaxPeers = 2;
            });
        }
    }
}