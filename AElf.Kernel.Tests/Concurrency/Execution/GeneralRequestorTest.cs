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
		private ActorSystem sys = ActorSystem.Create("test");
		private ChainContextServiceWithAdd _chainContextService;
		private ChainContextWithSmartContractZeroWithTransfer _chainContext;
		private ChainContextWithSmartContractZeroWithTransfer2 _chainContext2;
		private ProtobufSerializer _serializer = new ProtobufSerializer();
		private SmartContractZeroWithTransfer _smartContractZero { get { return (_chainContext.SmartContractZero as SmartContractZeroWithTransfer); } }
		private SmartContractZeroWithTransfer2 _smartContractZero2 { get { return (_chainContext2.SmartContractZero as SmartContractZeroWithTransfer2); } }
		private AccountContextService _accountContextService;
		private IActorRef _generalExecutor;

		public GeneralRequestorTest(
			ChainContextServiceWithAdd chainContextService,
			ChainContextWithSmartContractZeroWithTransfer chainContext,
			ChainContextWithSmartContractZeroWithTransfer2 chainContext2,
			AccountContextService accountContextService) : base(new XunitAssertions())
		{
			_chainContextService = chainContextService;
			_chainContext = chainContext;
            _chainContext2 = chainContext2;
			_accountContextService = accountContextService;
			_generalExecutor = sys.ActorOf(GeneralExecutor.Props(sys, _chainContextService, _accountContextService), "exec");
		}

		private Transaction GetTransaction(Hash from, Hash to, ulong qty)
		{
			// TODO: Test with IncrementId
			TransferArgs args = new TransferArgs()
			{
				From = from,
				To = to,
				Quantity = qty
			};

			ByteString argsBS = ByteString.CopyFrom(_serializer.Serialize(args));

			Transaction tx = new Transaction()
			{
				IncrementId = 0,
				From = from,
				To = to,
				MethodName = "Transfer",
				Params = argsBS
			};

			return tx;
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
				_smartContractZero.SetBalance(addbal.Item1, (ulong)addbal.Item2);
				_smartContractZero2.SetBalance(addbal.Item1, (ulong)addbal.Item2);
			}

			var txs1 = new List<ITransaction>(){
				GetTransaction(addresses[0], addresses[1], 10),
			};
			var txs2 = new List<ITransaction>(){
                GetTransaction(addresses[0], addresses[1], 20),
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

			_chainContextService.AddChainContext(_chainContext.ChainId, _chainContext);
			_generalExecutor.Tell(new RequestAddChainExecutor(_chainContext.ChainId));
            ExpectMsg<RespondAddChainExecutor>();

			_chainContextService.AddChainContext(_chainContext2.ChainId, _chainContext2);
            _generalExecutor.Tell(new RequestAddChainExecutor(_chainContext2.ChainId));
            ExpectMsg<RespondAddChainExecutor>();

			//var chainExecutor = sys.ActorOf(ParallelExecutionChainExecutor.Props(_chainContext, _accountContextService), "chainexecutor-" + _chainContext.ChainId.ToByteArray().ToHex());
			//var chainExecutor2 = sys.ActorOf(ParallelExecutionChainExecutor.Props(_chainContext2, _accountContextService), "chainexecutor-" + _chainContext2.ChainId.ToByteArray().ToHex());

			var requestor = sys.ActorOf(GeneralRequestor.Props(sys));

			var tcs = new TaskCompletionSource<List<TransactionResult>>();         
			requestor.Tell(new LocalExecuteTransactionsMessage(_chainContext.ChainId, txs1, tcs));
			tcs.Task.Wait();
			var tcs2 = new TaskCompletionSource<List<TransactionResult>>();
			requestor.Tell(new LocalExecuteTransactionsMessage(_chainContext2.ChainId, txs2, tcs2));
			tcs2.Task.Wait();

			foreach (var addFinbal in addresses.Zip(finalBalances1, Tuple.Create))
			{
				Assert.Equal((ulong)addFinbal.Item2, _smartContractZero.GetBalance(addFinbal.Item1));
			}

            foreach (var addFinbal in addresses.Zip(finalBalances2, Tuple.Create))
            {
                Assert.Equal((ulong)addFinbal.Item2, _smartContractZero2.GetBalance(addFinbal.Item1));
            }
		}
	}
}
