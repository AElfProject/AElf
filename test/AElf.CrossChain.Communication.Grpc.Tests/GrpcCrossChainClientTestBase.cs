using AElf.Kernel;
using AElf.TestBase;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcCrossChainClientTestBase : AElfIntegratedTest<GrpcCrossChainClientTestModule>
    {
        protected ChainOptions _chainOptions;

        public GrpcCrossChainClientTestBase()
        {
            _chainOptions = GetRequiredService<IOptionsSnapshot<ChainOptions>>().Value;
        }
    }
}