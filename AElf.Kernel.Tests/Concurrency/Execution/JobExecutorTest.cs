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
	public class JobExecutorTest : TestKitBase
	{
		private ActorSystem sys = ActorSystem.Create("test");
		private IChainContext _chainContext;
		private ProtobufSerializer _serializer = new ProtobufSerializer();

		public JobExecutorTest(ChainContextWithSmartContractZeroWithTransfer chainContext) : base(new XunitAssertions())
		{
			_chainContext = chainContext;
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
		public void ZeroTransactionExecutionTest()
		{
			var executor1 = sys.ActorOf(JobExecutor.Props(_chainContext, new List<ITransaction>(), TestActor));
            Watch(executor1);
			executor1.Tell(StartExecutionMessage.Instance);
            ExpectTerminated(executor1);
		}

		[Fact]
		public void SingleTransactionExecutionTest()
		{
			ProtobufSerializer serializer = new ProtobufSerializer();
			Hash from = Hash.Generate();
			Hash to = Hash.Generate();

			SmartContractZeroWithTransfer smartContractZero = (_chainContext.SmartContractZero as SmartContractZeroWithTransfer);
			smartContractZero.SetBalance(from, 100);
			smartContractZero.SetBalance(to, 0);

			// Normal transfer
			var tx1 = GetTransaction(from, to, 10);
			var executor1 = sys.ActorOf(JobExecutor.Props(_chainContext, new List<ITransaction>() { tx1 }, TestActor));
			Watch(executor1);
			executor1.Tell(StartExecutionMessage.Instance);
			var result = ExpectMsg<TransactionResultMessage>().TransactionResult;
			Assert.Equal(tx1.GetHash(), result.TransactionId);
			Assert.Equal(Status.Mined, result.Status);
			Assert.Equal((ulong)90, smartContractZero.GetBalance(from));
			Assert.Equal((ulong)10, smartContractZero.GetBalance(to));
			ExpectTerminated(executor1);

			// Insufficient balance
			var tx2 = GetTransaction(from, to, 100);
			var executor2 = ActorOf(JobExecutor.Props(_chainContext, new List<ITransaction>() { tx2 }, TestActor));
			executor2.Tell(StartExecutionMessage.Instance);
			result = ExpectMsg<TransactionResultMessage>().TransactionResult;
			Assert.Equal(Status.ExecutedFailed, result.Status);
			Assert.Equal((ulong)90, smartContractZero.GetBalance(from));
			Assert.Equal((ulong)10, smartContractZero.GetBalance(to));         
		}

		[Fact]
		public void MultipleTransactionExecutionTest()
		{
			ProtobufSerializer serializer = new ProtobufSerializer();
			Hash address1 = Hash.Generate();
			Hash address2 = Hash.Generate();
			Hash address3 = Hash.Generate();
			Hash address4 = Hash.Generate();


			SmartContractZeroWithTransfer smartContractZero = (_chainContext.SmartContractZero as SmartContractZeroWithTransfer);
			smartContractZero.SetBalance(address1, 100);
			smartContractZero.SetBalance(address2, 0);
			smartContractZero.SetBalance(address3, 200);
			smartContractZero.SetBalance(address4, 0);

			var tx1 = GetTransaction(address1, address2, 10);
			var tx2 = GetTransaction(address3, address4, 10);

			// Normal transfer
			var job1 = new List<ITransaction>{
				tx1,
				tx2
			};

			var executor1 = ActorOf(JobExecutor.Props(_chainContext, job1, TestActor));
			Watch(executor1);
			executor1.Tell(StartExecutionMessage.Instance);
			var result1 = ExpectMsg<TransactionResultMessage>().TransactionResult;
			var result2 = ExpectMsg<TransactionResultMessage>().TransactionResult;
			Assert.Equal(tx1.GetHash(), result1.TransactionId);
			Assert.Equal(tx2.GetHash(), result2.TransactionId);
			Assert.Equal(Status.Mined, result1.Status);
			Assert.Equal((ulong)90, smartContractZero.GetBalance(address1));
			Assert.Equal((ulong)10, smartContractZero.GetBalance(address2));
			Assert.Equal(Status.Mined, result2.Status);
			Assert.Equal((ulong)190, smartContractZero.GetBalance(address3));
			Assert.Equal((ulong)10, smartContractZero.GetBalance(address4));
			ExpectTerminated(executor1);

			// Check sequence
			TransferArgs args1 = (TransferArgs)serializer.Deserialize(tx1.Params.ToByteArray(), typeof(TransferArgs));
			TransferArgs args2 = (TransferArgs)serializer.Deserialize(tx2.Params.ToByteArray(), typeof(TransferArgs));
			var end1 = smartContractZero.TransactionEndTimes[args1];
			var start2 = smartContractZero.TransactionStartTimes[args2];
			Assert.True(end1 < start2);
		}
	}
}
