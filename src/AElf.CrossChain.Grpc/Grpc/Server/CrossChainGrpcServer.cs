using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Volo.Abp.Threading;

namespace AElf.CrossChain.Grpc
{
    public class CrossChainGrpcServer : ICrossChainServer
    {
        private Server _server;
        private readonly CrossChainGrpcServerBase _serverBase;
        public ILogger<CrossChainGrpcServer> Logger { get; set; }

        public CrossChainGrpcServer(CrossChainGrpcServerBase serverBase)
        {
            _serverBase = serverBase;
        }

        public async Task StartAsync(string localServerHost, int localServerPort)
        {
            _server = new global::Grpc.Core.Server
            {
                Services = {CrossChainRpc.BindService(_serverBase)},
                Ports =
                {
                    new ServerPort(localServerHost, localServerPort, ServerCredentials.Insecure)
                }
            };
            
            await Task.Run(() => _server.Start());
            
            Logger.LogDebug($"Grpc cross chain server started, listening at {localServerPort}");
        }

        public void Dispose()
        {
            if (_server == null)
                return;
            AsyncHelper.RunSync(() =>_server.ShutdownAsync());
            _server = null;
        }
    }
}