using AElf.TestBase;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace AElf.CrossChain.Grpc
{
    public class GrpcCrossChainServerTestBase : AElfIntegratedTest<GrpcCrossChainServerTestModule>
    {
        
    }
}