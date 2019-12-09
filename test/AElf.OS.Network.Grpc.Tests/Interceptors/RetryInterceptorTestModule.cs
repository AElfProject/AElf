using AElf.Modularity;
using AElf.OS.Network.Grpc;
using Volo.Abp.Modularity;

namespace AElf.OS.Network
{
    [DependsOn(typeof(GrpcNetworkModule))]
    public class RetryInterceptorTestModule : AElfModule
    {
    }
}