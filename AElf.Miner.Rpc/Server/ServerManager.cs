using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common.ByteArrayHelpers;
using AElf.Configuration;
using AElf.Configuration.Config.GRPC;
using AElf.Cryptography.Certificate;
using AElf.Miner.Rpc.Exceptions;
using Grpc.Core;
using NLog;

namespace AElf.Miner.Rpc.Server
{
    public class ServerManager
    {
        private Grpc.Core.Server _sideChainServer;
        private Grpc.Core.Server _parentChainServer;
        private CertificateStore _certificateStore;
        private SslServerCredentials _sslServerCredentials;
        private readonly ParentChainBlockInfoRpcServerImpl _parentChainBlockInfoRpcServerImpl;
        private readonly SideChainBlockInfoRpcServerImpl _sideChainBlockInfoRpcServerImpl;
        private readonly ILogger _logger;

        public ServerManager(ParentChainBlockInfoRpcServerImpl parentChainBlockInfoRpcServerImpl, 
            SideChainBlockInfoRpcServerImpl sideChainBlockInfoRpcServerImpl, ILogger logger)
        {
            _parentChainBlockInfoRpcServerImpl = parentChainBlockInfoRpcServerImpl;
            _sideChainBlockInfoRpcServerImpl = sideChainBlockInfoRpcServerImpl;
            _logger = logger;
            GrpcLocalConfig.ConfigChanged += GrpcLocalConfigOnConfigChanged;
        }

        private void GrpcLocalConfigOnConfigChanged(object sender, EventArgs e)
        {
            Init();
        }
        
        /// <summary>
        /// generate key-certificate pair from pem file 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="CertificateException"></exception>
        /// <exception cref="PrivateKeyException"></exception>
        private KeyCertificatePair GenerateKeyCertificatePair()
        {
            string ch = NodeConfig.Instance.ChainId;
            string certificate = _certificateStore.GetCertificate(ch);
            if(certificate == null)
                throw new CertificateException("Unable to load Certificate.");
            string privateKey = _certificateStore.GetPrivateKey(ch);
            if(privateKey == null)
                throw new PrivateKeyException("Unable to load private key.");
            return new KeyCertificatePair(certificate, privateKey);
        }
        
        /// <summary>
        /// create a new server
        /// </summary>
        /// <returns></returns>
        private Grpc.Core.Server CreateNewSideChainServer()
        {
            var server = new Grpc.Core.Server
            {
                Services = {SideChainBlockInfoRpc.BindService(_sideChainBlockInfoRpcServerImpl)},
                Ports =
                {
                    new ServerPort(GrpcLocalConfig.Instance.LocalServerIP, 
                        GrpcLocalConfig.Instance.LocalSideChainServerPort, _sslServerCredentials)
                }
            };

            return server;
        }

        /// <summary>
        /// create a new server
        /// </summary>
        /// <returns></returns>
        private Grpc.Core.Server CreateNewParentChainServer()
        {
            var server = new Grpc.Core.Server
            {
                Services = {ParentChainBlockInfoRpc.BindService(_parentChainBlockInfoRpcServerImpl)},
                Ports =
                {
                    new ServerPort(GrpcLocalConfig.Instance.LocalServerIP, 
                        GrpcLocalConfig.Instance.LocalParentChainServerPort, _sslServerCredentials)
                }
            };
            return server;
        }
        
        /// <summary>
        /// try to start host service as side chain miner
        /// this server is for indexing service
        /// </summary>
        /// <returns></returns>
        private async Task StartSideChainServer()
        {
            if(!GrpcLocalConfig.Instance.SideChainServer)
                return;

            // for safety, process all request before shutdown 
            await StopSideChainServer();
            _sideChainServer = CreateNewSideChainServer();
            _sideChainServer.Start();
            _logger.Debug("Started Side chain server at {0}", GrpcLocalConfig.Instance.LocalSideChainServerPort);
        }

        /// <summary>
        /// stop host service
        /// </summary>
        /// <returns></returns>
        private async Task StopSideChainServer()
        {
            if (_sideChainServer == null)
                return;
            await _sideChainServer.ShutdownAsync();
            _sideChainServer = null;
        }

        /// <summary>
        /// try to start host service as parent chain miner
        /// this server is for recording request from side chain miner
        /// </summary>
        /// <returns></returns>
        private async Task StartParentChainServer()
        {
            if(!GrpcLocalConfig.Instance.ParentChainServer)
                return;
            
            // for safety, process all request before shutdown 
            await StopParentChainServer();
            _parentChainServer = CreateNewParentChainServer();
            _parentChainServer.Start();
            _logger.Debug("Started Parent chain server at {0}", GrpcLocalConfig.Instance.LocalSideChainServerPort);
        }

        /// <summary>
        /// stop host service
        /// </summary>
        /// <returns></returns>
        private async Task StopParentChainServer()
        {
            if(_parentChainServer == null)
                return;
            await _parentChainServer.ShutdownAsync();
            _parentChainServer = null;
        }
        
        /// <summary>
        /// init pem storage
        /// and try to start servers if configuration set.
        /// </summary>
        /// <param name="dir"></param>
        public void Init(string dir = "")
        {
            _certificateStore = dir == "" ? _certificateStore : new CertificateStore(dir);
            var keyCertificatePair = GenerateKeyCertificatePair();
            // create credentials 
            _sslServerCredentials = new SslServerCredentials(new List<KeyCertificatePair> {keyCertificatePair});
            // start servers if possible 
            StartSideChainServer();
            StartParentChainServer();
        }

        /// <summary>
        /// stop host services
        /// </summary>
        public void Close()
        {
            StopSideChainServer();
            StopSideChainServer();
        }
        
    }
}