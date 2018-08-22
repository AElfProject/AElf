using System;
using AElf.Common.Attributes;
using AElf.Configuration.Config.GRPC;
using Grpc.Core;
using NLog;

namespace AElf.Miner.Rpc.Server
{
    [LoggerName("MinerServer")]
    public class MinerServer
    {
        private readonly HeaderInfoServerImpl _headerInfoServerImpl;
        private readonly ILogger _logger;
        private static readonly int Port = GrpcConfig.Instance.LocalMinerServerPort;
        private static readonly string Address = GrpcConfig.Instance.LocalMinerServerIP;
        private readonly Grpc.Core.Server _server;
        public MinerServer(ILogger logger, HeaderInfoServerImpl headerInfoServerImpl)
        {
            _logger = logger;
            _headerInfoServerImpl = headerInfoServerImpl;
            _server = new Grpc.Core.Server
            {
                Services = {HeaderInfoRpc.BindService(_headerInfoServerImpl)},
                Ports = {new ServerPort(Address, Port, ServerCredentials.Insecure)}
            };
        }

        public void StartUp()
        {
            _server.Start();
            _logger.Log(LogLevel.Debug, "Miner server listening on port " + Port);          
        }

        public void Stop()
        {
            _server.ShutdownAsync().Wait();
            _logger.Log(LogLevel.Debug, "Shutdowning miner server..");          

        }
    }
}