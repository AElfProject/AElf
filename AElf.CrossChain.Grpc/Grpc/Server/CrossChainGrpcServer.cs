using System;
using System.Collections.Generic;
using System.Linq;
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
using Volo.Abp.EventBus.Local;

namespace AElf.CrossChain.Grpc.Server
{
    public class CrossChainGrpcServer : ICrossChainServer
    {
        private global::Grpc.Core.Server _server;
        private readonly CrossChainGrpcServerBase _serverBase;
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
            await Task.Run(() => _server.Start());
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