using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace AElf.CrossChain.Grpc
{
    public class CrossChainGrpcServer : ICrossChainServer
    {
        private readonly CrossChainGrpcServerBase _serverBase;
        private Server _server;

        public CrossChainGrpcServer(CrossChainGrpcServerBase serverBase)
        {
            _serverBase = serverBase;
        }

        public ILogger<CrossChainGrpcServer> Logger { get; set; }

        public async Task StartAsync(string localServerIP, int localServerPort, KeyCertificatePair keyCert)
        {
            _server = new Server
            {
                Services = {CrossChainRpc.BindService(_serverBase)},
                Ports =
                {
                    new ServerPort(localServerIP, localServerPort,
                        new SslServerCredentials(new List<KeyCertificatePair> {keyCert}))
                }
            };
            _server.Start();

            Logger.LogDebug($"Grpc cross chain server started, listening at {localServerPort}");
        }

        public void Dispose()
        {
            if (_server == null)
                return;
            _server.ShutdownAsync();
            _server = null;
        }
    }
}