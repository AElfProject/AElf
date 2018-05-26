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
		private ActorSystem sys = ActorSystem.Create("test");
		private IActorRef _generalExecutor;
		private ChainContextServiceWithAdd _chainContextService;
		private ChainContextWithSmartContractZeroWithTransfer _chainContext;
		private ProtobufSerializer _serializer = new ProtobufSerializer();
		private SmartContractZeroWithTransfer _smartContractZero { get { return (_chainContext.SmartContractZero as SmartContractZeroWithTransfer); } }
		private AccountContextService _accountContextService;
      
		public ChainRequestorTest(ChainContextServiceWithAdd chainContextService, AccountContextService accountContextService, ChainContextWithSmartContractZeroWithTransfer chainContext) : base(new XunitAssertions())
		{
			_chainContextService = chainContextService;
			_chainContext = chainContext;
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
            }

            var txs = new List<ITransaction>(){
                GetTransaction(addresses[0], addresses[1], 10),
            };
            var txsHashes = txs.Select(y => y.GetHash()).ToList();

			var finalBalances = new List<int>
            {
                90, 10
            };

			//var chainExecutor = sys.ActorOf(ParallelExecutionChainExecutor.Props(_chainContext, _accountContextService), "chainexecutor-"+_chainContext.ChainId.ToByteArray().ToHex());
         
			_chainContextService.AddChainContext(_chainContext.ChainId, _chainContext);
			_generalExecutor.Tell(new RequestAddChainExecutor(_chainContext.ChainId));
			ExpectMsg<RespondAddChainExecutor>();

			var requestor = sys.ActorOf(ChainRequestor.Props(sys, _chainContext.ChainId));
   
			var tcs = new TaskCompletionSource<List<TransactionResult>>();
			requestor.Tell(new LocalExecuteTransactionsMessage(_chainContext.ChainId, txs, tcs));
			tcs.Task.Wait();
			foreach (var addFinbal in addresses.Zip(finalBalances, Tuple.Create))
            {
                Assert.Equal((ulong)addFinbal.Item2, _smartContractZero.GetBalance(addFinbal.Item1));
            }
        }
	}
}
