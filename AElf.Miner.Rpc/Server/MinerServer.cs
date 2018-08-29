using System.Collections.Generic;
using System.IO;
using AElf.Common.Application;
using AElf.Common.Attributes;
using AElf.Configuration.Config.GRPC;
using AElf.Cryptography.Certificate;
using AElf.Kernel;
using AElf.Miner.Rpc.Exceptions;
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

        private CertificateStore _certificateStore;
        private SslServerCredentials _sslServerCredentials;
        public MinerServer(ILogger logger, HeaderInfoServerImpl headerInfoServerImpl)
        {
            _logger = logger;
            _headerInfoServerImpl = headerInfoServerImpl;
        }

        public void Init(Hash chainId, string dir)
        {
            _certificateStore =
                new CertificateStore(dir);
            string ch = chainId.ToHex();
            string certificate = _certificateStore.GetCertificate(ch);
            if(certificate == null)
                throw new CertificateException("Unable to load Certificate.");
            string privateKey = _certificateStore.GetPrivateKey(ch);
            if(privateKey == null)
                throw new PrivateKeyException("Unable to load private key.");
            var keyCertificatePair = new KeyCertificatePair(certificate, privateKey);
            _sslServerCredentials = new SslServerCredentials(new List<KeyCertificatePair> {keyCertificatePair});
        }
        
        
        public void StartUp()
        {
            _server = new Grpc.Core.Server
            {
                Services = {HeaderInfoRpc.BindService(_headerInfoServerImpl)},
                Ports = {new ServerPort(Address, Port, _sslServerCredentials)}
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