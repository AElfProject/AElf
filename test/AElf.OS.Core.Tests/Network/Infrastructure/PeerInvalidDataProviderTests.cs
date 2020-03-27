using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using Shouldly;
using Xunit;

namespace AElf.OS.Network
{
    public class PeerInvalidDataProviderTests: NetworkInfrastructureTestBase
    {
        private readonly IPeerInvalidDataProvider _peerInvalidDataProvider;
        
        public PeerInvalidDataProviderTests()
        {
            _peerInvalidDataProvider = GetRequiredService<IPeerInvalidDataProvider>();
        }

        [Fact]
        public void MarkInvalidData_Test()
        {
            var host = "127.0.0.1";
            bool markResult;
            for (var i = 0; i < 50; i++)
            {
                markResult =_peerInvalidDataProvider.TryMarkInvalidData(host);
                markResult.ShouldBeTrue();
            }
            
            markResult =_peerInvalidDataProvider.TryMarkInvalidData(host);
            markResult.ShouldBeFalse();
            
            markResult =_peerInvalidDataProvider.TryMarkInvalidData("192.168.1.1");
            markResult.ShouldBeTrue();

            _peerInvalidDataProvider.TryRemoveInvalidData(host);
            
            markResult =_peerInvalidDataProvider.TryMarkInvalidData(host);
            markResult.ShouldBeTrue();
        }
    }
}