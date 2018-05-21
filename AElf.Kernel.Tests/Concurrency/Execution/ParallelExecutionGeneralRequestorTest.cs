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
	public class ParallelExecutionGeneralRequestorTest : TestKitBase
	{
		private ActorSystem sys = ActorSystem.Create("test");
		private ChainContextWithSmartContractZeroWithTransfer _chainContext;
		private ChainContextWithSmartContractZeroWithTransfer2 _chainContext2;
		private ProtobufSerializer _serializer = new ProtobufSerializer();
		private SmartContractZeroWithTransfer _smartContractZero { get { return (_chainContext.SmartContractZero as SmartContractZeroWithTransfer); } }
		private SmartContractZeroWithTransfer2 _smartContractZero2 { get { return (_chainContext2.SmartContractZero as SmartContractZeroWithTransfer2); } }
		private AccountContextService _accountContextService;

		public ParallelExecutionGeneralRequestorTest(
			ChainContextWithSmartContractZeroWithTransfer chainContext,
			ChainContextWithSmartContractZeroWithTransfer2 chainContext2,
			AccountContextService accountContextService) : base(new XunitAssertions())
		{
			_chainContext = chainContext;
            _chainContext2 = chainContext2;
			_accountContextService = accountContextService;         
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

			var txs1 = new List<Transaction>(){
				GetTransaction(addresses[0], addresses[1], 10),
			};
			var txs2 = new List<Transaction>(){
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

			var chainExecutor = sys.ActorOf(ParallelExecutionChainExecutor.Props(_chainContext, _accountContextService), "chainexecutor-" + _chainContext.ChainId.ToByteArray().ToHex());
			var chainExecutor2 = sys.ActorOf(ParallelExecutionChainExecutor.Props(_chainContext2, _accountContextService), "chainexecutor-" + _chainContext2.ChainId.ToByteArray().ToHex());

			var requestor = sys.ActorOf(ParallelExecutionGeneralRequestor.Props(sys));

			var tcs = new TaskCompletionSource<List<TransactionResult>>();         
			requestor.Tell(new LocalExecuteTransactionsMessage(_chainContext.ChainId, txs1, tcs));
			tcs.Task.Wait(TimeSpan.FromSeconds(3));
			var tcs2 = new TaskCompletionSource<List<TransactionResult>>();
			requestor.Tell(new LocalExecuteTransactionsMessage(_chainContext2.ChainId, txs2, tcs2));
			tcs2.Task.Wait(TimeSpan.FromSeconds(3));

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
