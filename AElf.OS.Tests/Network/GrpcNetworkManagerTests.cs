using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Configuration;
using AElf.Cryptography.ECDSA;
using AElf.OS.Network;
using AElf.OS.Network.Grpc;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AElf.OS.Tests.Network
{
    public class GrpcNetworkManagerTests
    {
        [Fact]
        public async Task Test()
        {
            //TODO: Remove it
            NodeConfig.Instance.ECKeyPair = new KeyPairGenerator().Generate();
            
            // Create a server
            NetworkOptions networkOptions = new NetworkOptions
            {
                ListeningPort = 6800
            };

            var mock = new Mock<IOptionsSnapshot<NetworkOptions>>();
            mock.Setup(m => m.Value).Returns(networkOptions);
            
            GrpcNetworkManager manager1 = new GrpcNetworkManager(mock.Object);
            await manager1.Start();
            
            // Create a peer
            NetworkOptions networkOptions2 = new NetworkOptions
            {
                BootNodes = new List<string> {"127.0.0.1:6800"},
                ListeningPort = 6801
            };

            var mock2 = new Mock<IOptionsSnapshot<NetworkOptions>>();
            mock2.Setup(m => m.Value).Returns(networkOptions2);
            
            GrpcNetworkManager manager2 = new GrpcNetworkManager(mock2.Object);
            await manager2.Start();

            await manager1.Stop();
            await manager2.Stop();
            
        }
    }
}