using AElf.CrossChain.Grpc.Server;
using AElf.Kernel;
using AElf.TestBase;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain.Grpc.Client
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
    
    public class GrpcCrossChainClientWithoutParentChainTestBase : AElfIntegratedTest<GrpcCrossChainClientWithoutParentChainTestModule>
    {
        protected ChainOptions ChainOptions;
        protected IGrpcCrossChainServer Server;
        
        public GrpcCrossChainClientWithoutParentChainTestBase()
        {
            ChainOptions = GetRequiredService<IOptionsSnapshot<ChainOptions>>().Value;
            Server = GetRequiredService<IGrpcCrossChainServer>();
        }
    }
}