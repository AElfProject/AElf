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
	public class ParallelExecutionJobExecutorTest : TestKitBase
	{
		private ActorSystem sys = ActorSystem.Create("test");
		private IChainContext _chainContext;
		private ProtobufSerializer _serializer = new ProtobufSerializer();

		public ParallelExecutionJobExecutorTest(ChainContextWithSmartContractZeroWithTransfer chainContext) : base(new XunitAssertions())
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
		public void JobExecutionTest()
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
			var job1 = new List<Transaction>{
				tx1,
				tx2
			};

			var executor1 = ActorOf(ParallelExecutionJobExecutor.Props(_chainContext, job1, TestActor));
			Watch(executor1);
			executor1.Tell(new StartExecutionMessage());
			var results = ExpectMsg<JobResultMessage>().TransactionResults.ToDictionary(x => x.TransactionId);//new TimeSpan(0, 0, 10)
			Assert.Contains(tx1.GetHash(), results.Keys);
			Assert.Contains(tx2.GetHash(), results.Keys);
			var result1 = results[tx1.GetHash()];
			var result2 = results[tx2.GetHash()];
			Assert.Equal(Status.Mined, result1.Status);
			Assert.Equal((ulong)90, smartContractZero.GetBalance(address1));
			Assert.Equal((ulong)10, smartContractZero.GetBalance(address2));
			Assert.Equal(Status.Mined, result2.Status);
			Assert.Equal((ulong)190, smartContractZero.GetBalance(address3));
			Assert.Equal((ulong)10, smartContractZero.GetBalance(address4));
			ExpectTerminated(executor1);

            // Check sequence
			TransferArgs args1 = (TransferArgs) serializer.Deserialize(tx1.Params.ToByteArray(), typeof(TransferArgs));
			TransferArgs args2 = (TransferArgs)serializer.Deserialize(tx2.Params.ToByteArray(), typeof(TransferArgs));
			var end1 = smartContractZero.TransactionEndTimes[args1];
			var start2 = smartContractZero.TransactionStartTimes[args2];
			Assert.True(end1 < start2);      
		}

	}
}
