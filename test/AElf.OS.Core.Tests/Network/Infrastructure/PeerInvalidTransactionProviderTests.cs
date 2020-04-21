using System.Threading.Tasks;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using Shouldly;
using Xunit;

namespace AElf.OS.Network
{
    public class PeerInvalidTransactionProviderTests: NetworkInfrastructureTestBase
    {
        private readonly IPeerInvalidTransactionProvider _peerInvalidTransactionProvider;
        
        public PeerInvalidTransactionProviderTests()
        {
            _peerInvalidTransactionProvider = GetRequiredService<IPeerInvalidTransactionProvider>();
        }

        [Fact]
        public void MarkInvalidTransaction_Test()
        {
            var host = "127.0.0.1";
            bool markResult;
            for (var i = 0; i < 5; i++)
            {
                markResult =_peerInvalidTransactionProvider.TryMarkInvalidTransaction(host);
                markResult.ShouldBeTrue();
            }
            
            markResult =_peerInvalidTransactionProvider.TryMarkInvalidTransaction(host);
            markResult.ShouldBeFalse();
            
            markResult =_peerInvalidTransactionProvider.TryMarkInvalidTransaction("192.168.1.1");
            markResult.ShouldBeTrue();

            _peerInvalidTransactionProvider.TryRemoveInvalidRecord(host);
            
            markResult =_peerInvalidTransactionProvider.TryMarkInvalidTransaction(host);
            markResult.ShouldBeTrue();
        }
        
        [Fact]
        public async Task MarkInvalidTransaction_Timeout_Test()
        {
            var host = "127.0.0.1";
            bool markResult;
            for (var i = 0; i < 5; i++)
            {
                markResult =_peerInvalidTransactionProvider.TryMarkInvalidTransaction(host);
                markResult.ShouldBeTrue();
            }
            
            markResult =_peerInvalidTransactionProvider.TryMarkInvalidTransaction(host);
            markResult.ShouldBeFalse();
            
            markResult =_peerInvalidTransactionProvider.TryMarkInvalidTransaction("192.168.1.1");
            markResult.ShouldBeTrue();

            await Task.Delay(1500);
            
            markResult =_peerInvalidTransactionProvider.TryMarkInvalidTransaction(host);
            markResult.ShouldBeTrue();
        }
    }
}