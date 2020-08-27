using System.Threading.Tasks;
using AElf.Kernel.TransactionPool;
using AElf.OS.Network.Infrastructure;
using Shouldly;
using Xunit;

namespace AElf.OS.Handlers
{
    public class TransactionExecutionValidationFailedEventHandlerTests : TransactionValidationFailedEventHandlerTestBase
    {
        private readonly TransactionExecutionValidationFailedEventHandler
            _transactionExecutionValidationFailedEventHandler;
        private readonly IPeerInvalidTransactionProvider _peerInvalidTransactionProvider;
        private readonly IPeerPool _peerPool;
        
        public TransactionExecutionValidationFailedEventHandlerTests()
        {
            _transactionExecutionValidationFailedEventHandler =
                GetRequiredService<TransactionExecutionValidationFailedEventHandler>();
            _peerInvalidTransactionProvider = GetRequiredService<IPeerInvalidTransactionProvider>();
            _peerPool = GetRequiredService<IPeerPool>();
        }

        [Fact]
        public async Task HandleEvent_Test()
        {
            for (var i = 0; i < 5; i++)
            {
                var invalidTransactionId = HashHelper.ComputeFrom("Tx" + i + "Peer1");
                await _transactionExecutionValidationFailedEventHandler.HandleEventAsync(
                    new TransactionExecutionValidationFailedEvent {TransactionId = invalidTransactionId});
            }
            await Task.Delay(500);

            var peer = _peerPool.FindPeerByPublicKey("Peer1");
            _peerInvalidTransactionProvider.TryMarkInvalidTransaction(peer.RemoteEndpoint.Host,
                HashHelper.ComputeFrom("Tx" + 5 + "Peer1")).ShouldBeFalse();
        }
    }
}