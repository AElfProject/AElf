using System.Threading.Tasks;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Infrastructure
{
    public class PeerInvalidTransactionProviderTests : NetworkInfrastructureTestBase
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
            
            _peerInvalidTransactionProvider.TryRemoveInvalidRecord(host).ShouldBe(false);
            
            for (var i = 0; i < 5; i++)
            {
                var txId = HashHelper.ComputeFrom(i.ToString());
                markResult = _peerInvalidTransactionProvider.TryMarkInvalidTransaction(host, txId);
                markResult.ShouldBeTrue();
            }

            markResult = _peerInvalidTransactionProvider.TryMarkInvalidTransaction(host, HashHelper.ComputeFrom("0"));
            markResult.ShouldBeTrue();

            markResult = _peerInvalidTransactionProvider.TryMarkInvalidTransaction(host, HashHelper.ComputeFrom("5"));
            markResult.ShouldBeFalse();

            markResult =
                _peerInvalidTransactionProvider.TryMarkInvalidTransaction("192.168.1.1", HashHelper.ComputeFrom("0"));
            markResult.ShouldBeTrue();

            _peerInvalidTransactionProvider.TryRemoveInvalidRecord(host).ShouldBe(true);

            markResult = _peerInvalidTransactionProvider.TryMarkInvalidTransaction(host, HashHelper.ComputeFrom("0"));
            markResult.ShouldBeTrue();
        }

        [Fact]
        public async Task MarkInvalidTransaction_Timeout_Test()
        {
            var host = "127.0.0.1";
            bool markResult;
            for (var i = 0; i < 5; i++)
            {
                var txId = HashHelper.ComputeFrom(i.ToString());
                markResult = _peerInvalidTransactionProvider.TryMarkInvalidTransaction(host, txId);
                markResult.ShouldBeTrue();
            }

            markResult = _peerInvalidTransactionProvider.TryMarkInvalidTransaction(host, HashHelper.ComputeFrom("5"));
            markResult.ShouldBeFalse();

            await Task.Delay(1500);

            markResult = _peerInvalidTransactionProvider.TryMarkInvalidTransaction(host, HashHelper.ComputeFrom("6"));
            markResult.ShouldBeTrue();
        }
    }
}