using System;
using System.Threading.Tasks;
using AElf.Network.Config;
using AElf.Network.Exceptions;
using Xunit;

namespace AElf.Network.Tests.Server
{
    public class AElfTcpServerUnitTests
    {
        /* Testing Start method */

        [Fact]
        public async Task Start_ShouldThrow_NullConf()
        {
            AElfTcpServer server = new AElfTcpServer(null, null);

            Exception ex =
                await Assert.ThrowsAsync<ServerConfigurationException>(async () => await server.StartAsync());
            Assert.Equal("Could not start the server, config object is null.", ex.Message);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("127.0..1")]
        public async Task Start_ShouldThrow_BadIp(string ip)
        {
            AElfNetworkConfig conf = new AElfNetworkConfig { Host = ip };
            AElfTcpServer server = new AElfTcpServer(conf, null);

            Exception ex =
                await Assert.ThrowsAsync<ServerConfigurationException>(async () => await server.StartAsync());

            Assert.Equal("Could not start the server, invalid ip.", ex.Message);
        }
    }
}