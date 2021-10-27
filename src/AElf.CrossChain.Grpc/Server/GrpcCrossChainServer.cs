using System.Net;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Volo.Abp.Threading;

namespace AElf.CrossChain.Grpc.Server
{
    public class GrpcCrossChainServer : IGrpcCrossChainServer
    {
        private global::Grpc.Core.Server _server;
        private readonly GrpcParentChainServerBase _grpcParentChainServerBase;
        private readonly GrpcSideChainServerBase _grpcSideChainServerBase;
        private readonly GrpcBasicServerBase _grpcBasicServerBase;

        public GrpcCrossChainServer(GrpcParentChainServerBase grpcParentChainServerBase,
            GrpcSideChainServerBase grpcSideChainServerBase, GrpcBasicServerBase grpcBasicServerBase)
        {
            _grpcParentChainServerBase = grpcParentChainServerBase;
            _grpcSideChainServerBase = grpcSideChainServerBase;
            _grpcBasicServerBase = grpcBasicServerBase;
        }

        public ILogger<GrpcCrossChainServer> Logger { get; set; }

        public async Task StartAsync(int listeningPort)
        {
            _server = new global::Grpc.Core.Server
            {
                Ports =
                {
                    new ServerPort(IPAddress.Any.ToString(), listeningPort, ServerCredentials.Insecure)
                },
                Services =
                {
                    ParentChainRpc.BindService(_grpcParentChainServerBase),
                    SideChainRpc.BindService(_grpcSideChainServerBase),
                    BasicCrossChainRpc.BindService(_grpcBasicServerBase)
                }
            };

            await Task.Run(() => _server.Start());

            Logger.LogInformation($"Grpc cross chain server started, listening at {listeningPort}");
            IsStarted = true;
        }

        public bool IsStarted { get; private set; }

        public void Dispose()
        {
            if (_server == null)
                return;
            AsyncHelper.RunSync(() => _server.ShutdownAsync());
            _server = null;
            IsStarted = false;
        }
    }
}