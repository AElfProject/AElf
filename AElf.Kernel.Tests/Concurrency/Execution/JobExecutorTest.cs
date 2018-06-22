﻿using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using Xunit.Frameworks.Autofac;
using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Execution.Messages;

namespace AElf.Kernel.Tests.Concurrency.Execution
{

    [UseAutofacTestFramework]
    public class JobExecutorTest : TestKitBase
    {
        private MockSetup _mock;
        private ActorSystem sys = ActorSystem.Create("test");
        private IActorRef _serviceRouter;

        public JobExecutorTest(MockSetup mock) : base(new XunitAssertions())
        {
            _mock = mock;
            _serviceRouter = sys.ActorOf(LocalServicesProvider.Props(_mock.ServicePack));
        }

        [Fact]
        public async Task ServicePackTest()
        {
            var sp = _mock.ServicePack;
            var chainContext = await _mock.ServicePack.ChainContextService.GetChainContextAsync(_mock.ChainId1);
            Assert.NotNull(chainContext);
        }

        [Fact]
        public void ZeroTransactionExecutionTest()
        {
            var executor1 = sys.ActorOf(JobExecutor.Props(_mock.ChainId1, _serviceRouter, new List<ITransaction>(), TestActor));
            Watch(executor1);
            executor1.Tell(StartExecutionMessage.Instance);
            ExpectTerminated(executor1);
        }

        [Fact]
        public void SingleTransactionExecutionTest()
        {
            Hash from = Hash.Generate();
            Hash to = Hash.Generate();
            Hash chainId = _mock.ChainId1;

            _mock.Initialize1(from, 100);
            _mock.Initialize1(to, 0);

            // Normal transfer
            var tx1 = _mock.GetTransferTxn1(from, to, 10);
            var executor1 = sys.ActorOf(JobExecutor.Props(chainId, _serviceRouter, new List<ITransaction>() { tx1 }, TestActor));
            Watch(executor1);
            executor1.Tell(StartExecutionMessage.Instance);
            var trace = ExpectMsg<TransactionTraceMessage>().TransactionTrace;

            Assert.Equal(tx1.GetHash(), trace.TransactionId);
            Assert.True(string.IsNullOrEmpty(trace.StdErr));

            Assert.Equal((ulong)90, _mock.GetBalance1(from));
            Assert.Equal((ulong)10, _mock.GetBalance1(to));
            ExpectTerminated(executor1);
        }

        [Fact]
        public void MultipleTransactionExecutionTest()
        {
            Hash address1 = Hash.Generate();
            Hash address2 = Hash.Generate();
            Hash address3 = Hash.Generate();
            Hash address4 = Hash.Generate();

            Hash chainId = _mock.ChainId1;

            _mock.Initialize1(address1, 100);
            _mock.Initialize1(address2, 0);
            _mock.Initialize1(address3, 200);
            _mock.Initialize1(address4, 0);

            var tx1 = _mock.GetTransferTxn1(address1, address2, 10);
            var tx2 = _mock.GetTransferTxn1(address3, address4, 10);

            // Normal transfer
            var job1 = new List<ITransaction>{
                tx1,
                tx2
            };

            var executor1 = sys.ActorOf(JobExecutor.Props(chainId, _serviceRouter, job1, TestActor));
            Watch(executor1);
            executor1.Tell(StartExecutionMessage.Instance);
            var trace1 = ExpectMsg<TransactionTraceMessage>().TransactionTrace;
            var trace2 = ExpectMsg<TransactionTraceMessage>().TransactionTrace;

            Assert.Equal(tx1.GetHash(), trace1.TransactionId);
            Assert.Equal(tx2.GetHash(), trace2.TransactionId);
            Assert.True(string.IsNullOrEmpty(trace1.StdErr));
            Assert.Equal((ulong)90, _mock.GetBalance1(address1));
            Assert.Equal((ulong)10, _mock.GetBalance1(address2));
            Assert.True(string.IsNullOrEmpty(trace2.StdErr));
            Assert.Equal((ulong)190, _mock.GetBalance1(address3));
            Assert.Equal((ulong)10, _mock.GetBalance1(address4));
            ExpectTerminated(executor1);

            // Check sequence
            var end1 = _mock.GetTransactionEndTime1(tx1);
            var start2 = _mock.GetTransactionStartTime1(tx2);
            Assert.True(end1 < start2);
        }
    }
}
