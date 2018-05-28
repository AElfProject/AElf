using System;
using System.Threading.Tasks;
using AElf.Kernel.Node.Network;
using AElf.Kernel.Node.Network.Exceptions;
using Xunit;

namespace AElf.Kernel.Tests.Network.Server
{
    public class AElfTcpServerUnitTests
    {
        /* Testing Start method */

        [Fact]
        public async Task Start_ShouldThrow_NullConf()
        {
            AElfTcpServer server = new AElfTcpServer(null, null);
            
            Exception ex = await Assert.ThrowsAsync<ServerConfigurationException>(async () => await server.Start());
            Assert.Equal("Could not start the server, config object is null", ex.Message);
        }

        [Fact]
        public void Start_ShouldThrow_BadIp()
        {
            //todo
        }
    }
}