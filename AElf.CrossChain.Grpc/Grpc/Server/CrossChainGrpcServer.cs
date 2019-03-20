using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography.Certificate;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

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

        public async Task StartAsync(string localServerIP, int localServerPort, KeyCertificatePair keyCert)
        {
            _server = new global::Grpc.Core.Server
            {
                Services = {CrossChainRpc.BindService(_serverBase)},
                Ports =
                {
                    new ServerPort(localServerIP, localServerPort, new SslServerCredentials(new List<KeyCertificatePair> {keyCert}))
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