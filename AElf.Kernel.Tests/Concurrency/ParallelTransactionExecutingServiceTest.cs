using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Frameworks.Autofac;
using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using AElf.Kernel.Concurrency;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Tests.Concurrency.Execution;

namespace AElf.Kernel.Tests.Concurrency
{
	[UseAutofacTestFramework]
	public class ParallelTransactionExecutingServiceTest : TestKitBase
	{
        private IActorRef _generalExecutor;
        private MockSetup _mock;
        private ActorSystem sys = ActorSystem.Create("test");
        private IActorRef _serviceRouter;

        public ParallelTransactionExecutingServiceTest(MockSetup mock) : base(new XunitAssertions())
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

			var service = new ParallelTransactionExecutingService(sys);

			var traces = Task.Factory.StartNew(async () =>
			{
                return await service.ExecuteAsync(txs, _mock.ChainId1);
			}).Unwrap().Result;
			foreach (var txTrace in txs.Zip(traces, Tuple.Create))
			{
				Assert.Equal(txTrace.Item1.GetHash(), txTrace.Item2.TransactionId);
				Assert.True(string.IsNullOrEmpty(txTrace.Item2.StdErr));
			}
			foreach (var addFinbal in addresses.Zip(finalBalances, Tuple.Create))
			{
                Assert.Equal((ulong)addFinbal.Item2, _mock.GetBalance1(addFinbal.Item1));
			}
		}
	}
}
