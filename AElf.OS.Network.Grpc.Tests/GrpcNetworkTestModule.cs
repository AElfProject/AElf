using AElf.Modularity;
using AElf.OS.Network.Grpc;
using Volo.Abp.Modularity;

namespace AElf.OS.Network
{
    [DependsOn(typeof(OSCoreTestAElfModule), typeof(GrpcNetworkModule))]
    public class GrpcNetworkTestModule : AElfModule
    {
        
    }
}