using System.Threading.Tasks;
using AElf.Cryptography.Certificate;
using AElf.Kernel.Node.Infrastructure;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain.Grpc
{
    public class GrpcCrossChainServerNodePlugin : INodePlugin
    {
        private readonly GrpcCrossChainConfigOption _grpcCrossChainConfigOption;
        private readonly ICertificateStore _certificateStore;
        private readonly ICrossChainServer _crossChainServer;

        public GrpcCrossChainServerNodePlugin(IOptionsSnapshot<GrpcCrossChainConfigOption> grpcCrossChainConfigOption, 
            ICertificateStore certificateStore, ICrossChainServer crossChainServer)
        {
            _grpcCrossChainConfigOption = grpcCrossChainConfigOption.Value;
            _certificateStore = certificateStore;
            _crossChainServer = crossChainServer;
        }

        public Task StartAsync(int chainId)
        {
            if (!_grpcCrossChainConfigOption.LocalServer)
                return Task.CompletedTask;
            var keyPair = LoadKeyPair(_grpcCrossChainConfigOption.LocalCertificateFileName);
            return _crossChainServer.StartAsync(_grpcCrossChainConfigOption.LocalServerIP,
                _grpcCrossChainConfigOption.LocalServerPort, keyPair);
        }

        public Task ShutdownAsync()
        {
            _crossChainServer.Dispose();
            return Task.CompletedTask;
        }
        
        private KeyCertificatePair LoadKeyPair(string fileName)
        {
            var keyStore = _certificateStore.LoadKeyStore(fileName);
            var cert = _certificateStore.LoadCertificate(fileName);
            return new KeyCertificatePair(cert, keyStore);
        }
    }
}