using AElf.TestBase;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace AElf.OS.Network
{
    public class GrpcNetworkTestBase : AElfIntegratedTest<GrpcNetworkTestModule>
    {
    }
    
    public class ServerServiceTestBase : AElfIntegratedTest<ConnectionServiceTestModule>
    {
    }

    public class GrpcBasicNetworkTestBase : AElfIntegratedTest<GrpcBasicNetworkTestModule>
    {
    }
}