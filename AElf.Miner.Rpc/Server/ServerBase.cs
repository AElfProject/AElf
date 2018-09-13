using System.Collections.Generic;
using AElf.Common.Application;
using AElf.Configuration.Config.GRPC;
using AElf.Cryptography.Certificate;
using AElf.Kernel;
using AElf.Miner.Rpc.Exceptions;
using Grpc.Core;
using NLog;

namespace AElf.Miner.Rpc.Server
{
    public abstract class ServerBase
    {
        private readonly ILogger _logger;
        private Grpc.Core.Server _server;
        private CertificateStore _certificateStore;
        private SslServerCredentials _sslServerCredentials;
        private Hash _chainId;
        
        /// <summary>
        /// server is started if true
        /// </summary>
        public bool On { get; set; }

        protected ServerBase(ILogger logger)
        {
            _logger = logger;
        }

        public void Init(Hash chainId, string dir = "")
        {
            _certificateStore = new CertificateStore(dir == "" ? ApplicationHelpers.GetDefaultDataDir() : dir);
            _chainId = chainId;
            
            //generate key certificate Pair
            var keyCertificatePair = GenerateKeyCertificatePair();
            // create credential
            _sslServerCredentials = new SslServerCredentials(new List<KeyCertificatePair> {keyCertificatePair});
            
            // init server impl
            InitServerImpl(chainId);
            
            _logger?.Debug("Init server..");
        }
        
        protected void StartUp(string address, int port)
        {
            if(On)
                return;
            _server = new Grpc.Core.Server
            {
                Services = {BindService()},
                Ports =
                {
                    new ServerPort(address, port, _sslServerCredentials)
                }
            };
            _server.Start();
            On = true;
            _logger.Log(LogLevel.Debug, "Listening on " + address + ":" + port);
        }

        public bool ReStartUp()
        {
            if (!On)
                return false;
            Stop();
            //generate key certificate Pair
            var keyCertificatePair = GenerateKeyCertificatePair();
            // create credential
            _sslServerCredentials = new SslServerCredentials(new List<KeyCertificatePair> {keyCertificatePair});
            return true;
        }
        
        public void Stop()
        {
            if (!On)
                return;
            _server.ShutdownAsync().Wait();
            _logger.Log(LogLevel.Debug, "Shutdowning miner server..");
        }

        private KeyCertificatePair GenerateKeyCertificatePair()
        {
            string ch = _chainId.ToHex();
            string certificate = _certificateStore.GetCertificate(ch);
            if(certificate == null)
                throw new CertificateException("Unable to load Certificate.");
            string privateKey = _certificateStore.GetPrivateKey(ch);
            if(privateKey == null)
                throw new PrivateKeyException("Unable to load private key.");
            return new KeyCertificatePair(certificate, privateKey);
        }

        public abstract void StartUp();
        protected abstract ServerServiceDefinition BindService();
        protected abstract void InitServerImpl(Hash chainId);
    }
}