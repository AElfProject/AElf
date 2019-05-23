using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CrossChain.Grpc;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Volo.Abp.Threading;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcGrpcCrossChainServer : IGrpcCrossChainServer
    {
        private Server _server;
        private readonly IEnumerable<CrossChainRpc.CrossChainRpcBase> _serverBases;
        public ILogger<GrpcGrpcCrossChainServer> Logger { get; set; }

        public GrpcGrpcCrossChainServer(IEnumerable<CrossChainRpc.CrossChainRpcBase> serverBases)
        {
            _serverBases = serverBases;
        }

        public async Task StartAsync(string localServerHost, int localServerPort)
        {
            _server = new Server
            {
                Ports =
                {
                    new ServerPort(localServerHost, localServerPort, ServerCredentials.Insecure)
                }
            };
            foreach (var serverBase in _serverBases)
            {
                var serviceDefinition = CrossChainRpc.BindService(serverBase);
                _server.Services.Add(serviceDefinition);
            }
        
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