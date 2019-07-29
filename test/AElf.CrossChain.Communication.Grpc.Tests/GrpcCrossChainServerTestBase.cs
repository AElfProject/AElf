using AElf.Kernel;
using AElf.TestBase;
using Microsoft.Extensions.Options;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcCrossChainServerTestBase : AElfIntegratedTest<GrpcCrossChainServerTestModule>
    {
        protected ChainOptions _chainOptions;

        public GrpcCrossChainServerTestBase()
        {
            _chainOptions = GetRequiredService<IOptionsSnapshot<ChainOptions>>().Value;
        }
    }
}