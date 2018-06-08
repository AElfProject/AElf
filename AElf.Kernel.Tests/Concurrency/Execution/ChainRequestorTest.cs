using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Frameworks.Autofac;
using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using AElf.Kernel;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Services;
using AElf.Kernel.Extensions;
using Google.Protobuf;

namespace AElf.Kernel.Tests.Concurrency.Execution
{
    [UseAutofacTestFramework]
    public class ChainRequestorTest : TestKitBase
    {
        private IActorRef _generalExecutor;
        private MockSetup _mock;
        private ActorSystem sys = ActorSystem.Create("test");
        private IActorRef _serviceRouter;

        public ChainRequestorTest(MockSetup mock) : base(new XunitAssertions())
        {
            _mock = mock;
            _serviceRouter = sys.ActorOf(LocalServicesProvider.Props(_mock.ServicePack));
            _generalExecutor = sys.ActorOf(GeneralExecutor.Props(sys, _serviceRouter), "exec");
        }

        [Fact]
        public void Test()
        {
            var balances = new List<int>()
            {
                100, 0
            };
            var addresses = Enumerable.Range(0, balances.Count).Select(x => Hash.Generate()).ToList();

            foreach (var addbal in addresses.Zip(balances, Tuple.Create))
            {
                _mock.Initialize1(addbal.Item1, (ulong)addbal.Item2);
            }

            var txs = new List<ITransaction>(){
                _mock.GetTransferTxn1(addresses[0], addresses[1], 10),
            };
            var txsHashes = txs.Select(y => y.GetHash()).ToList();

            var finalBalances = new List<int>
            {
                90, 10
            };

            _generalExecutor.Tell(new RequestAddChainExecutor(_mock.ChainId1));
            ExpectMsg<RespondAddChainExecutor>();

            var requestor = sys.ActorOf(ChainRequestor.Props(sys, _mock.ChainId1));

            var tcs = new TaskCompletionSource<List<TransactionResult>>();
            requestor.Tell(new LocalExecuteTransactionsMessage(_mock.ChainId1, txs, tcs));
            tcs.Task.Wait();
            foreach (var addFinbal in addresses.Zip(finalBalances, Tuple.Create))
            {
                Assert.Equal((ulong)addFinbal.Item2, _mock.GetBalance1(addFinbal.Item1));
            }
        }
    }
}
