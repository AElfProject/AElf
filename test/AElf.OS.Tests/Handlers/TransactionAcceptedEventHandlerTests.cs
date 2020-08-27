using System;
using System.Threading.Tasks;
using AElf.Kernel.TransactionPool;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.Types;
using Moq;
using Xunit;

namespace AElf.OS.Handlers
{
    public class TransactionAcceptedEventHandlerTests : NetworkBroadcastTestBase
    {
        private readonly TransactionAcceptedEventHandler _transactionAcceptedEventHandler;
        private readonly NetworkServicePropagationTestContext _testContext;
        private readonly OSTestHelper _osTestHelper;

        public TransactionAcceptedEventHandlerTests()
        {
            _transactionAcceptedEventHandler = GetRequiredService<TransactionAcceptedEventHandler>();
            _testContext = GetRequiredService<NetworkServicePropagationTestContext>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
        }

        [Fact]
        public async Task HandleEvent_Test()
        {
            var transaction = await _osTestHelper.GenerateTransferTransaction();
            await _transactionAcceptedEventHandler.HandleEventAsync(new TransactionAcceptedEvent
            {
                Transaction = transaction
            });
            await Task.Delay(500);

            foreach (var peer in _testContext.MockedPeers)
                peer.Verify(p => p.EnqueueTransaction(It.Is<Transaction>(tx => tx.GetHash() == transaction.GetHash()),
                    It.IsAny<Action<NetworkException>>()), Times.Once());
        }
    }
}