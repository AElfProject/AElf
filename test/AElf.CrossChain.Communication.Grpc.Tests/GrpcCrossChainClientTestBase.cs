using AElf.Kernel;
using AElf.TestBase;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcCrossChainClientTestBase : AElfIntegratedTest<GrpcCrossChainClientTestModule>
    {
        protected ChainOptions ChainOptions;
        protected IGrpcCrossChainServer Server;
        
        public GrpcCrossChainClientTestBase()
        {
            ChainOptions = GetRequiredService<IOptionsSnapshot<ChainOptions>>().Value;
            Server = GetRequiredService<IGrpcCrossChainServer>();
        }
    }
}