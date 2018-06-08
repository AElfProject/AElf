using System;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using Xunit.Frameworks.Autofac;
using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Services;
using Google.Protobuf;

namespace AElf.Kernel.Tests.Concurrency.Execution
{
	[UseAutofacTestFramework]
	public class ChainExecutorTest : TestKitBase
	{
        private MockSetup _mock;
        private ActorSystem sys = ActorSystem.Create("test");
        private IActorRef _serviceRouter;

        public ChainExecutorTest(MockSetup mock) : base(new XunitAssertions())
        {
            _mock = mock;
            _serviceRouter = sys.ActorOf(LocalServicesProvider.Props(_mock.ServicePack));
        }

		[Fact]
		public void RequestExecuteTransactionsTest()
		{
			/*
             *  Job 1: (0-1, 10), (1-2, 9)
             *  Job 2: (3-4, 8)
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
                _mock.GetTransferTxn1(addresses[3], addresses[4], 8)
            };
            var txsHashes = txs.Select(y => y.GetHash()).ToList();

            var finalBalances = new List<int>
            {
                90, 1, 9, 192, 8
            };

            var chainExecutor = sys.ActorOf(ChainExecutor.Props(_mock.ChainId1, _serviceRouter));

			chainExecutor.Tell(new RequestExecuteTransactions(33, txs));
			var respond = ExpectMsg<RespondExecuteTransactions>();
			Assert.Equal(33, respond.RequestId);
			Assert.Equal(RespondExecuteTransactions.RequestStatus.Executed, respond.Status);
			var results = respond.TransactionResults;
            Assert.Equal(Status.Mined, results[0].Status);
            Assert.Equal(Status.Mined, results[1].Status);
            Assert.Equal(Status.Mined, results[2].Status);
            foreach (var addFinbal in addresses.Zip(finalBalances, Tuple.Create))
            {
                Assert.Equal((ulong)addFinbal.Item2, _mock.GetBalance1(addFinbal.Item1));
            }
		}
	}
}