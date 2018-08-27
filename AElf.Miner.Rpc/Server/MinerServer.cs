using System;
using System.Collections.Generic;
using System.IO;
using AElf.Common.Application;
using AElf.Common.Attributes;
using AElf.Configuration.Config.GRPC;
using AElf.Kernel;
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
        private Grpc.Core.Server _server;
        public MinerServer(ILogger logger, HeaderInfoServerImpl headerInfoServerImpl)
        {
            _logger = logger;
            _headerInfoServerImpl = headerInfoServerImpl;
            
        }

        public void StartUp(Hash chainId)
        {
            string certificate = File.ReadAllText(ApplicationHelpers.GetDefaultDataDir() + "/certs/" + "mainchain_cert.pem");
            string privateKey = File.ReadAllText(ApplicationHelpers.GetDefaultDataDir() + "/certs/" + "mainchain_key.pem");
            string crt = File.ReadAllText(ApplicationHelpers.GetDefaultDataDir() + "/certs/" + "sidechain_cert.pem");

            var keyCertificatePair = new KeyCertificatePair(certificate, privateKey);
            var credentials = new SslServerCredentials(new List<KeyCertificatePair> {keyCertificatePair});
            
            _server = new Grpc.Core.Server
            {
                Services = {HeaderInfoRpc.BindService(_headerInfoServerImpl)},
                Ports = {new ServerPort(Address, Port, credentials)}
            };
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