using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Grpc
{
    public interface IGrpcServerContext
    {
        Task StartServerAsync();
        Task StopServerAsync();

        void RegisterConnectionCallback(Func<string, ConnectionInfo, Task<ConnectReply>> connectionCallback);
        void RegisterHandshakeCallback(Func<string, Handshake, Task<HandshakeReply>> handshakeCallback);

        Task<ConnectionInfo> GetConnectionInfoAsync();
    }

    public class GrpcServerContext : IGrpcServerContext, ISingletonDependency
    {
        private ChainOptions ChainOptions => ChainOptionsSnapshot.Value;
        public IOptionsSnapshot<ChainOptions> ChainOptionsSnapshot { get; set; }
        private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
        public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }
        
        private IAccountService _accountService;
        
        private Server _server;
        private GrpcServerService _serverService;

        public GrpcServerContext(Server server, GrpcServerService serverService, IAccountService accountService)
        {
            _server = server;
            _serverService = serverService;
            _accountService = accountService;
        }

        public async Task StartServerAsync()
        {
            // start listening
            await Task.Run(() => _server.Start());
        }

        public async Task StopServerAsync()
        {
            await _server.KillAsync();
        }
        
        public void RegisterConnectionCallback(Func<string, ConnectionInfo, Task<ConnectReply>> connectionCallback)
        {
            _serverService.RegisterConnectionCallback(connectionCallback);
        }

        public void RegisterHandshakeCallback(Func<string, Handshake, Task<HandshakeReply>> handshakeCallback)
        {
            _serverService.RegisterHandshakeCallback(handshakeCallback);
        }
        
        public async Task<ConnectionInfo> GetConnectionInfoAsync()
        {
            return new ConnectionInfo
            {
                ChainId = ChainOptions.ChainId,
                ListeningPort = NetworkOptions.ListeningPort,
                Version = KernelConstants.ProtocolVersion,
                Pubkey = ByteString.CopyFrom(await _accountService.GetPublicKeyAsync())
            };
        }
    }
}