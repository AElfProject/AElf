using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.OS.Network.Grpc
{
    [DependsOn(typeof(GrpcNetworkModule))]
    public class RetryInterceptorTestModule : AElfModule
    {
        
    }
}