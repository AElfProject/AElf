using System.Threading.Tasks;
using AElf.CrossChain.Communication;

namespace AElf.CrossChain.Grpc.Client
{
    public interface IGrpcClientPlugin : ICrossChainCommunicationPlugin
    {
        Task CreateClientAsync(GrpcCrossChainClientCreationContext grpcCrossChainClientCreationContext);
    }
}