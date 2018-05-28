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
		private ActorSystem sys = ActorSystem.Create("test");
		private ChainContextWithSmartContractZeroWithTransfer _chainContext;
		private AccountContextService _accountContextService;

		public ChainExecutorTest(ChainContextWithSmartContractZeroWithTransfer chainContext, AccountContextService accountContextService) : base(new XunitAssertions())
		{
			_chainContext = chainContext;
			_accountContextService = accountContextService;
		}

		[Fact]
		public void RequestAccountDataContextTest()
		{
			Hash accountHash = Hash.Generate();
			var chainExecutor = sys.ActorOf(ChainExecutor.Props(_chainContext, _accountContextService));

			chainExecutor.Tell(new RequestAccountDataContext(42, accountHash));
			var accountDataContext = ExpectMsg<RespondAccountDataContext>();
			Assert.Equal(42, accountDataContext.RequestId);
			Assert.Equal(accountHash, accountDataContext.AccountDataContext.Address);
            Assert.Equal((ulong)0, accountDataContext.AccountDataContext.IncrementId);

			var localAccountDataContext = _accountContextService.GetAccountDataContext(accountHash, _chainContext.ChainId);
            localAccountDataContext.Result.IncrementId += 1;

			chainExecutor.Tell(new RequestAccountDataContext(43, accountHash));
			accountDataContext = ExpectMsg<RespondAccountDataContext>();
            Assert.Equal((ulong)1, accountDataContext.AccountDataContext.IncrementId);
		}

		#region RequestExecuteTransactionsTest
		private ProtobufSerializer _serializer = new ProtobufSerializer();
		private SmartContractZeroWithTransfer _smartContractZero { get { return (_chainContext.SmartContractZero as SmartContractZeroWithTransfer); } }

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

        private DateTime GetTransactionStartTime(Transaction tx)
        {
            TransferArgs args = (TransferArgs)_serializer.Deserialize(tx.Params.ToByteArray(), typeof(TransferArgs));
            return _smartContractZero.TransactionStartTimes[args];
        }

        private DateTime GetTransactionEndTime(Transaction tx)
        {
            TransferArgs args = (TransferArgs)_serializer.Deserialize(tx.Params.ToByteArray(), typeof(TransferArgs));
            return _smartContractZero.TransactionEndTimes[args];
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
                _smartContractZero.SetBalance(addbal.Item1, (ulong)addbal.Item2);
            }

            var txs = new List<ITransaction>(){
                GetTransaction(addresses[0], addresses[1], 10),
                GetTransaction(addresses[1], addresses[2], 9),
                GetTransaction(addresses[3], addresses[4], 8)
            };
            var txsHashes = txs.Select(y => y.GetHash()).ToList();

            var finalBalances = new List<int>
            {
                90, 1, 9, 192, 8
            };

			var chainExecutor = sys.ActorOf(ChainExecutor.Props(_chainContext, _accountContextService));

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
                Assert.Equal((ulong)addFinbal.Item2, _smartContractZero.GetBalance(addFinbal.Item1));
            }
		}
		#endregion RequestExecuteTransactionsTest
	}
}