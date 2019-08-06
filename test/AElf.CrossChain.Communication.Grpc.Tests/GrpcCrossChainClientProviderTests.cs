using System;
using System.Threading.Tasks;
using AElf.CrossChain.Communication.Infrastructure;
using Xunit;

namespace AElf.CrossChain.Communication.Grpc
{
    public class GrpcCrossChainClientProviderTests : GrpcCrossChainClientTestBase
    {
        private readonly ICrossChainClientProvider _grpcCrossChainClientProvider;

        public GrpcCrossChainClientProviderTests()
        {
            _grpcCrossChainClientProvider = GetRequiredService<ICrossChainClientProvider>();
        }

        [Fact]
        public void AddOrUpdateClient_Test()
        {
            var remoteChainId = ChainOptions.ChainId;
            var localChainId = ChainHelper.GetChainId(1);

            var host = "127.0.0.1";
            var port = 5000;
            var crossChainClientDto = new CrossChainClientDto
            {
                LocalChainId = localChainId,
                RemoteChainId = remoteChainId,
                IsClientToParentChain = false,
                RemoteServerHost = host,
                RemoteServerPort = port
            };
            
            var client = _grpcCrossChainClientProvider.AddOrUpdateClient(crossChainClientDto);

            Assert.True(client.RemoteChainId == remoteChainId);
            Assert.False(client.IsConnected);
            Assert.Equal(remoteChainId, client.RemoteChainId);

            var expectedUriStr = string.Concat(host, ":", "5000");
            Assert.Equal(expectedUriStr, client.TargetUriString);
        }
        
        
        [Fact]
        public void TryGetClient_Test()
        {
            var remoteChainId = ChainOptions.ChainId;
            var localChainId = ChainHelper.GetChainId(1);

            var host = "127.0.0.1";
            var port = 5000;
            var crossChainClientDto = new CrossChainClientDto
            {
                LocalChainId = localChainId,
                RemoteChainId = remoteChainId,
                IsClientToParentChain = false,
                RemoteServerHost = host,
                RemoteServerPort = port
            };
            
            var client = _grpcCrossChainClientProvider.AddOrUpdateClient(crossChainClientDto);
            var isClientCached = _grpcCrossChainClientProvider.TryGetClient(remoteChainId, out var clientInCache);
            Assert.True(isClientCached);
            Assert.Equal(client, clientInCache);
        }
        
        [Fact]
        public void GetAllClients_Test()
        {
            var remoteChainId = ChainOptions.ChainId;
            var localChainId = ChainHelper.GetChainId(1);

            var host = "127.0.0.1";
            var port = 5000;
            var crossChainClientDto = new CrossChainClientDto
            {
                LocalChainId = localChainId,
                RemoteChainId = remoteChainId,
                IsClientToParentChain = false,
                RemoteServerHost = host,
                RemoteServerPort = port
            };
            
            var client = _grpcCrossChainClientProvider.AddOrUpdateClient(crossChainClientDto);

            var clients = _grpcCrossChainClientProvider.GetAllClients();
            Assert.Single(clients);
            Assert.Equal(client, clients[0]);
        }
        
                
        [Fact]
        public void CreateCrossChainClient_Test()
        {
            var remoteChainId = ChainOptions.ChainId;
            var localChainId = ChainHelper.GetChainId(1);

            var host = "127.0.0.1";
            var port = 5000;
            var crossChainClientDto = new CrossChainClientDto
            {
                LocalChainId = localChainId,
                RemoteChainId = remoteChainId,
                IsClientToParentChain = false,
                RemoteServerHost = host,
                RemoteServerPort = port
            };
            
            var client = _grpcCrossChainClientProvider.CreateCrossChainClient(crossChainClientDto);
            var isClientCached = _grpcCrossChainClientProvider.TryGetClient(remoteChainId, out _);
            Assert.False(isClientCached);
            
            Assert.True(client.RemoteChainId == remoteChainId);
            Assert.False(client.IsConnected);
            Assert.Equal(remoteChainId, client.RemoteChainId);
            
            var expectedUriStr = string.Concat(host, ":", "5000");
            Assert.Equal(expectedUriStr, client.TargetUriString);
        }
    }
}