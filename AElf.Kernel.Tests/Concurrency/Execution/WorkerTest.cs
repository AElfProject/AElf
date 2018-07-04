using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;
using Xunit.Frameworks.Autofac;
using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Execution.Messages;
using NServiceKit.Common.Extensions;

namespace AElf.Kernel.Tests.Concurrency.Execution
{
    [UseAutofacTestFramework]
    public class WorkerTest : TestKitBase
    {
        private MockSetup _mock;

        public WorkerTest(MockSetup mock) : base(new XunitAssertions())
        {
            _mock = mock;
        }

        [Fact]
        public void SingleTransactionExecutionTest()
        {
            Hash from = Hash.Generate();
            Hash to = Hash.Generate();

            _mock.Initialize1(from, 100);
            _mock.Initialize1(to, 0);

            // Normal transfer
            var tx1 = _mock.GetTransferTxn1(from, to, 10);

            _mock.Worker1.Tell(new JobExecutionRequest(0, _mock.ChainId1, new List<ITransaction>() {tx1}, TestActor,
                TestActor));

            // Start processing
            var js1 = ExpectMsg<JobExecutionStatus>();
            Assert.Equal(JobExecutionStatus.RequestStatus.Running, js1.Status);

            // Return result
            var trace = ExpectMsg<TransactionTraceMessage>().TransactionTraces.FirstOrDefault();

            // Completed, two messages will be received
            // 1 for sender, 1 for router (in this test both are TestActor)
            ExpectMsg<JobExecutionStatus>();
            var js2 = ExpectMsg<JobExecutionStatus>();
            Assert.Equal(JobExecutionStatus.RequestStatus.Completed, js2.Status);

            Assert.Equal(tx1.GetHash(), trace.TransactionId);
            if (!string.IsNullOrEmpty(trace.StdErr))
            {
                Assert.Null(trace.StdErr);
            }

            Assert.Equal((ulong) 90, _mock.GetBalance1(from));
            Assert.Equal((ulong) 10, _mock.GetBalance1(to));

            // Query status
            _mock.Worker1.Tell(new JobExecutionStatusQuery(0));

            // Invalid request id as it has already completed
            var js3 = ExpectMsg<JobExecutionStatus>();
            Assert.Equal(JobExecutionStatus.RequestStatus.InvalidRequestId, js3.Status);
        }

        [Fact]
        public void MultipleTransactionExecutionTest()
        {
            Hash address1 = Hash.Generate();
            Hash address2 = Hash.Generate();
            Hash address3 = Hash.Generate();
            Hash address4 = Hash.Generate();

            _mock.Initialize1(address1, 100);
            _mock.Initialize1(address2, 0);
            _mock.Initialize1(address3, 200);
            _mock.Initialize1(address4, 0);

            var tx1 = _mock.GetTransferTxn1(address1, address2, 10);
            var tx2 = _mock.GetTransferTxn1(address3, address4, 10);

            // Normal transfer
            var job1 = new List<ITransaction>
            {
                tx1,
                tx2
            };

            _mock.Worker1.Tell(new JobExecutionRequest(0, _mock.ChainId1, job1, TestActor,
                TestActor));

            // Start processing
            var js1 = ExpectMsg<JobExecutionStatus>();
            Assert.Equal(JobExecutionStatus.RequestStatus.Running, js1.Status);

            // Return result
            var traces = ExpectMsg<TransactionTraceMessage>().TransactionTraces;

            // Completed
            var js2 = ExpectMsg<JobExecutionStatus>();
            Assert.Equal(JobExecutionStatus.RequestStatus.Completed, js2.Status);

            Assert.Equal(tx1.GetHash(), traces[0].TransactionId);
            Assert.Equal(tx2.GetHash(), traces[1].TransactionId);
            if (!string.IsNullOrEmpty(traces[0].StdErr))
            {
                Assert.Null(traces[0].StdErr);
            }
            Assert.Equal((ulong) 90, _mock.GetBalance1(address1));
            Assert.Equal((ulong) 10, _mock.GetBalance1(address2));
            if (!string.IsNullOrEmpty(traces[1].StdErr))
            {
                Assert.Null(traces[1].StdErr);
            }
            Assert.Equal((ulong) 190, _mock.GetBalance1(address3));
            Assert.Equal((ulong) 10, _mock.GetBalance1(address4));

            // Check sequence
            var end1 = _mock.GetTransactionEndTime1(tx1);
            var start2 = _mock.GetTransactionStartTime1(tx2);
            Assert.True(end1 < start2);
        }

        [Fact]
        public void JobCancelTest()
        {
            var job = new List<ITransaction>()
            {
                _mock.GetSleepTxn1(1000),
                _mock.GetSleepTxn1(1000),
                _mock.GetNoActionTxn1()
            };

            _mock.Worker1.Tell(new JobExecutionRequest(0, _mock.ChainId1, job, TestActor,
                TestActor));

            Thread.Sleep(1500);
            _mock.Worker1.Tell(JobExecutionCancelMessage.Instance);

            var traces = new List<TransactionTrace>();
            traces.AddRange(((TransactionTraceMessage) FishForMessage(msg => msg is TransactionTraceMessage)).TransactionTraces);

            Assert.Equal("Execution Cancelled", traces[2].StdErr);
        }
    }
}