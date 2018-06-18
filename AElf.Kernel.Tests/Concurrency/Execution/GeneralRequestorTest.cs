using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Frameworks.Autofac;
using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Services;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;
using Google.Protobuf;

namespace AElf.Kernel.Tests.Concurrency.Execution
{

	[UseAutofacTestFramework]
	public class GeneralRequestorTest : TestKitBase
	{
        private IActorRef _generalExecutor;
        private MockSetup _mock;
        private ActorSystem sys = ActorSystem.Create("test");
        private IActorRef _serviceRouter;

        public GeneralRequestorTest(MockSetup mock) : base(new XunitAssertions())
        {
            _mock = mock;
            _serviceRouter = sys.ActorOf(LocalServicesProvider.Props(_mock.ServicePack));
            _generalExecutor = sys.ActorOf(GeneralExecutor.Props(sys, _serviceRouter), "exec");
        }

		[Fact]
		public void Test()
		{
			var balances = new List<ulong>()
			{
				100, 0
			};
			var addresses = Enumerable.Range(0, balances.Count).Select(x => Hash.Generate()).ToList();

			foreach (var addbal in addresses.Zip(balances, Tuple.Create))
			{
                _mock.Initialize1(addbal.Item1, (ulong)addbal.Item2);
                _mock.Initialize2(addbal.Item1, (ulong)addbal.Item2);
			}

			var txs1 = new List<ITransaction>(){
                _mock.GetTransferTxn1(addresses[0], addresses[1], 10),
			};
			var txs2 = new List<ITransaction>(){
                _mock.GetTransferTxn2(addresses[0], addresses[1], 20),
            };
			var txsHashes1 = txs1.Select(y => y.GetHash()).ToList();
			var txsHashes2 = txs2.Select(y => y.GetHash()).ToList();

			var finalBalances1 = new List<int>
			{
				90, 10
			};

			var finalBalances2 = new List<int>
            {
                80, 20
            };

            _generalExecutor.Tell(new RequestAddChainExecutor(_mock.ChainId1));
            ExpectMsg<RespondAddChainExecutor>();

            _generalExecutor.Tell(new RequestAddChainExecutor(_mock.ChainId2));
            ExpectMsg<RespondAddChainExecutor>();

			var requestor = sys.ActorOf(GeneralRequestor.Props(sys));

			var tcs = new TaskCompletionSource<List<TransactionTrace>>();         
            requestor.Tell(new LocalExecuteTransactionsMessage(_mock.ChainId1, txs1, tcs));
			tcs.Task.Wait();
			var tcs2 = new TaskCompletionSource<List<TransactionTrace>>();
            requestor.Tell(new LocalExecuteTransactionsMessage(_mock.ChainId2, txs2, tcs2));
			tcs2.Task.Wait();
			
			foreach (var addFinbal in addresses.Zip(finalBalances1, Tuple.Create))
			{
                Assert.Equal((ulong)addFinbal.Item2, _mock.GetBalance1(addFinbal.Item1));
			}

            foreach (var addFinbal in addresses.Zip(finalBalances2, Tuple.Create))
            {
                Assert.Equal((ulong)addFinbal.Item2, _mock.GetBalance2(addFinbal.Item1));
            }
		}
	}
}
