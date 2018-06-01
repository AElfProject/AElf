using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using Xunit.Frameworks.Autofac;
using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using Google.Protobuf;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Execution.Messages;

namespace AElf.Kernel.Tests.Concurrency.Execution
{

    [UseAutofacTestFramework]
    public class GroupExecutorTest : TestKitBase
    {
        private MockSetup _mock;
        private ActorSystem sys = ActorSystem.Create("test");
        private IActorRef _serviceRouter;

        public GroupExecutorTest(MockSetup mock) : base(new XunitAssertions())
        {
            _mock = mock;
            _serviceRouter = sys.ActorOf(LocalServicesProvider.Props(_mock.ServicePack));
        }

        [Fact]
        public void TwoBatchGroupExecutionTest()
        {
            /*
             *  Batch 1:
             *    Job 1: (0-1, 10), (1-2, 9)
             *    Job 2: (3-4, 8)
             *  Batch 2:
             *    Job 1: (0-3, 7)
             */

            var balances = new List<int>()
            {
                100, 0, 0, 200, 0
            };
            var addresses = Enumerable.Range(0, balances.Count).Select(x => Hash.Generate()).ToList();

            foreach (var addbal in addresses.Zip(balances, Tuple.Create))
            {
                _mock.Initialize1(addbal.Item1, (ulong)addbal.Item2);
            }

            var txs = new List<ITransaction>(){
                _mock.GetTransferTxn1(addresses[0], addresses[1], 10),
                _mock.GetTransferTxn1(addresses[1], addresses[2], 9),
                _mock.GetTransferTxn1(addresses[3], addresses[4], 8),
                _mock.GetTransferTxn1(addresses[0], addresses[3], 7),
            };
            var txsHashes = txs.Select(y => y.GetHash()).ToList();

            var finalBalances = new List<int>
            {
                83, 1, 9, 199, 8
            };

            var executor1 = sys.ActorOf(GroupExecutor.Props(_mock.ChainId1, _serviceRouter, txs, TestActor));
            Watch(executor1);
            executor1.Tell(StartExecutionMessage.Instance);
            var results = new List<TransactionResult>()
            {
                ExpectMsg<TransactionResultMessage>().TransactionResult,
                ExpectMsg<TransactionResultMessage>().TransactionResult,
                ExpectMsg<TransactionResultMessage>().TransactionResult,
                ExpectMsg<TransactionResultMessage>().TransactionResult,
            }.OrderBy(y => txsHashes.IndexOf(y.TransactionId)).ToList();
            ExpectTerminated(executor1);

            // Tx3 starts after all other Tx's are finished
            foreach (var tx in txs.GetRange(0, 3))
            {
                Assert.True(_mock.GetTransactionStartTime1(txs[3]) > _mock.GetTransactionEndTime1(tx));
            }
            foreach (var r in results)
            {
                Assert.Equal(Status.Mined, r.Status);
            }
            foreach (var addFinbal in addresses.Zip(finalBalances, Tuple.Create))
            {
                Assert.Equal((ulong)addFinbal.Item2, _mock.GetBalance1(addFinbal.Item1));
            }
        }
    }
}
