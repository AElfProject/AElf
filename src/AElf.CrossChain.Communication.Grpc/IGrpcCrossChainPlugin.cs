using System.Threading.Tasks;

namespace AElf.CrossChain.Communication.Grpc
{
    public interface IGrpcCrossChainPlugin
    {
        Task StartAsync(int chainId);
        Task StopAsync();
    }

    public interface IGrpcClientPlugin : IGrpcCrossChainPlugin
    {
        Task CreateClientAsync(GrpcCrossChainClientDto grpcCrossChainClientDto);
    }

    public interface IGrpcServePlugin : IGrpcCrossChainPlugin
    {
    }
}