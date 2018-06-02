using System;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using AElf.Network;
using AElf.Network.Config;
using AElf.Network.Exceptions;
using Xunit;

namespace AElf.Kernel.Tests.Network.Server
{
    public class PeerManagerUnitTests
    {
        [Fact]
        public async Task Start_ShouldThrow_NullServer()
        {
            IAElfServer _server = null;

            Exception ex = 
                await Assert.ThrowsAsync<NullReferenceException>(async () => await _server.StartAsync());
            Assert.Equal("Could not start the server, server object is null.", ex.Message);
        }
        
    }
}