using System;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Node.Infrastructure;
using Microsoft.Extensions.Options;
using Xunit;

namespace AElf.CrossChain.Grpc
{
    public sealed class GrpcCrossChainClientNodePluginTest : GrpcCrossChainClientTestBase
    {
        private readonly INodePlugin _grpcCrossChainServerNodePlugin;
        private readonly GrpcCrossChainClientNodePlugin _grpcCrossChainClientNodePlugin;
        private readonly ChainOptions _chainOptions;
        private readonly GrpcCrossChainConfigOption _grpcCrossChainConfigOption;

        public GrpcCrossChainClientNodePluginTest()
        {
            _grpcCrossChainServerNodePlugin = GetRequiredService<INodePlugin>();
            _grpcCrossChainClientNodePlugin = GetRequiredService<GrpcCrossChainClientNodePlugin>();
            _chainOptions = GetRequiredService<IOptionsSnapshot<ChainOptions>>().Value;
            _grpcCrossChainConfigOption = GetRequiredService<IOptionsSnapshot<GrpcCrossChainConfigOption>>().Value;
        }

        [Fact]
        public async Task Server_Start_Test()
        {
            var chainId = _chainOptions.ChainId;
            await _grpcCrossChainServerNodePlugin.StartAsync(chainId);
        }
        
        [Fact]
        public async Task Client_Start_Test()
        {
            var chainId = _chainOptions.ChainId;
            await _grpcCrossChainClientNodePlugin.StartAsync(chainId);
        }

        [Fact]
        public async Task GrpcServeNewChainReceivedEventTest()
        {
            var receivedEventData = new GrpcCrossChainRequestReceivedEvent
            {
                LocalChainId = _chainOptions.ChainId,
                CrossChainCommunicationContextDto = new GrpcCrossChainCommunicationDto
                {
                    RemoteChainId = ChainHelpers.ConvertBase58ToChainId("ETH"),
                    IsClientToParentChain = true,
                    RemoteIp = _grpcCrossChainConfigOption.RemoteParentChainNodeIp,
                    RemotePort = _grpcCrossChainConfigOption.RemoteParentChainNodePort,
                    LocalChainId = _chainOptions.ChainId,
                    LocalListeningPort = _grpcCrossChainConfigOption.LocalServerPort
                }
            };
            await _grpcCrossChainClientNodePlugin.HandleEventAsync(receivedEventData);
        }

        [Fact]
        public async Task Client_Shutdown_Test()
        {
            //TODO: Add test cases for GrpcCrossChainClientNodePlugin.ShutdownAsync after it is implemented [Case]
            await Assert.ThrowsAsync<NotImplementedException>(()=>_grpcCrossChainClientNodePlugin.ShutdownAsync()); 
        }

        public override void Dispose()
        {
            _grpcCrossChainServerNodePlugin?.ShutdownAsync().Wait();
        }
    }
}