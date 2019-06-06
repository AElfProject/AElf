using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Volo.Abp.Threading;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcGrpcCrossChainServer : IGrpcCrossChainServer
    {
        private Server _server;
        private readonly GrpcParentChainServerBase _grpcParentChainServerBase;
        private readonly GrpcSideChainServerBase _grpcSideChainServerBase;
        private readonly GrpcBasicServerBase _grpcBasicServerBase;

        public GrpcGrpcCrossChainServer(GrpcParentChainServerBase grpcParentChainServerBase, 
            GrpcSideChainServerBase grpcSideChainServerBase, GrpcBasicServerBase grpcBasicServerBase)
        {
            _grpcParentChainServerBase = grpcParentChainServerBase;
            _grpcSideChainServerBase = grpcSideChainServerBase;
            _grpcBasicServerBase = grpcBasicServerBase;
        }

        public ILogger<GrpcGrpcCrossChainServer> Logger { get; set; }
        
        public async Task StartAsync(string localServerHost, int localServerPort)
        {
            _server = new Server
            {
                Ports =
                {
                    new ServerPort(localServerHost, localServerPort, ServerCredentials.Insecure)
                },
                Services =
                {
                    ParentChainRpc.BindService(_grpcParentChainServerBase),
                    SideChainRpc.BindService(_grpcSideChainServerBase), 
                    BasicCrossChainRpc.BindService(_grpcBasicServerBase)
                }
            };
//            foreach (var serverBase in _serverBases)
//            {
//                var serviceDefinition = CrossChainRpc.BindService(serverBase);
//                _server.Services.Add(serviceDefinition);
//            }
        
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