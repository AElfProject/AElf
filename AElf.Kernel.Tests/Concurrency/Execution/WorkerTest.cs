using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Frameworks.Autofac;
using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using AElf.Execution;

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

            _mock.Worker1.Tell(new JobExecutionRequest(0, _mock.ChainId1, new List<Transaction>() {tx1}, TestActor,
                TestActor));

/*
 Temporarily disabled.
 TODO: https://github.com/AElfProject/AElf/issues/338
            // Start processing
            var js1 = ExpectMsg<JobExecutionStatus>();
            Assert.Equal(JobExecutionStatus.RequestStatus.Running, js1.Status);
*/
            // Return result
            var trace = ExpectMsg<TransactionTraceMessage>().TransactionTraces.FirstOrDefault();

/*
 Temporarily disabled.
 TODO: https://github.com/AElfProject/AElf/issues/338
            // Completed, two messages will be received
            // 1 for sender, 1 for router (in this test both are TestActor)
            ExpectMsg<JobExecutionStatus>();
            var js2 = ExpectMsg<JobExecutionStatus>();
            Assert.Equal(JobExecutionStatus.RequestStatus.Completed, js2.Status);
*/
            Assert.Equal(tx1.GetHash(), trace.TransactionId);
            if (!string.IsNullOrEmpty(trace.StdErr))
            {
                Assert.Null(trace.StdErr);
            }

            Assert.Equal((ulong) 90, _mock.GetBalance1(from));
            Assert.Equal((ulong) 10, _mock.GetBalance1(to));

/*
 Temporarily disabled.
 TODO: https://github.com/AElfProject/AElf/issues/338
            // Query status
            _mock.Worker1.Tell(new JobExecutionStatusQuery(0));

            // Invalid request id as it has already completed
            var js3 = ExpectMsg<JobExecutionStatus>();
            Assert.Equal(JobExecutionStatus.RequestStatus.InvalidRequestId, js3.Status);
*/
        }

        [Fact]
        public void MultipleTransactionExecutionTest()
        {
            var address1 = Hash.Generate();
            var address2 = Hash.Generate();
            var address3 = Hash.Generate();
            var address4 = Hash.Generate();

            _mock.Initialize1(address1, 100);
            _mock.Initialize1(address2, 0);
            _mock.Initialize1(address3, 200);
            _mock.Initialize1(address4, 0);

            var tx1 = _mock.GetTransferTxn1(address1, address2, 10);
            var tx2 = _mock.GetTransferTxn1(address3, address4, 10);

            // Normal transfer
            var job1 = new List<Transaction>
            {
                tx1,
                tx2
            };

            _mock.Worker1.Tell(new JobExecutionRequest(0, _mock.ChainId1, job1, TestActor, TestActor));

            // Start processing

/*
 Temporarily disabled.
 TODO: https://github.com/AElfProject/AElf/issues/338
            var js1 = ExpectMsg<JobExecutionStatus>();
            Assert.Equal(JobExecutionStatus.RequestStatus.Running, js1.Status);
*/
            // Return result
            var trace = ExpectMsg<TransactionTraceMessage>().TransactionTraces;
            var trace1 = trace[0];
            var trace2 = trace[1];

            // Completed

/*
 Temporarily disabled.
 TODO: https://github.com/AElfProject/AElf/issues/338
            var js2 = ExpectMsg<JobExecutionStatus>();
            Assert.Equal(JobExecutionStatus.RequestStatus.Completed, js2.Status);
*/
            Assert.Equal(tx1.GetHash(), trace1.TransactionId);
            Assert.Equal(tx2.GetHash(), trace2.TransactionId);
            if (!string.IsNullOrEmpty(trace1.StdErr))
            {
                Assert.Null(trace1.StdErr);
            }
            Assert.Equal((ulong) 90, _mock.GetBalance1(address1));
            Assert.Equal((ulong) 10, _mock.GetBalance1(address2));
            if (!string.IsNullOrEmpty(trace2.StdErr))
            {
                Assert.Null(trace2.StdErr);
            }
            Assert.Equal((ulong) 190, _mock.GetBalance1(address3));
            Assert.Equal((ulong) 10, _mock.GetBalance1(address4));

            // Check sequence
            var end1 = _mock.GetTransactionEndTime1(tx1);
            var start2 = _mock.GetTransactionStartTime1(tx2);
            Assert.True(end1 < start2);
        }

/*
 Temporarily disabled.
 TODO: https://github.com/AElfProject/AElf/issues/338
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

            var traces = new List<TransactionTrace>()
            {
                ((TransactionTraceMessage) FishForMessage(msg => msg is TransactionTraceMessage))
                .TransactionTrace,
                ((TransactionTraceMessage) FishForMessage(msg => msg is TransactionTraceMessage))
                .TransactionTrace,
                ((TransactionTraceMessage) FishForMessage(msg => msg is TransactionTraceMessage))
                .TransactionTrace
            };

            Assert.Equal("Execution Cancelled", traces[2].StdErr);
        }
*/
    }
}