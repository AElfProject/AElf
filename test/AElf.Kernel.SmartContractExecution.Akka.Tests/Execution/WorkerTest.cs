//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Xunit;
//using Akka.Actor;
//using Akka.TestKit.Xunit;
//using AElf.Kernel.SmartContractExecution;
//using AElf.Types;
//using AElf.Kernel.SmartContractExecution.Execution;
//using Google.Protobuf;
//using Address= AElf.Common.Address;

namespace AElf.Kernel.Tests.Concurrency.Execution
{
    /*
    public class WorkerTest : AElfAkkaTestKitBase
    {
        private MockSetup _mock;

        public WorkerTest() : base(new XunitAssertions())
        {
            _mock = _aelfKernelIntegratedTest.GetRequiredService<MockSetup>();
        }

        [Fact]
        public void SingleTransactionExecutionTest()
        {
            Address from = Address.FromString(nameof(SingleTransactionExecutionTest)+"/from");
            Address to = Address.FromString(nameof(SingleTransactionExecutionTest)+"/to");

            _mock.Initialize1(from, 100);
            _mock.Initialize1(to, 0);

            // Normal transfer
            var tx1 = _mock.GetTransferTxn1(from, to, 10);

            _mock.Worker1.Tell(new JobExecutionRequest(0, _mock.ChainId1, new List<Transaction>() {tx1}, TestActor,
                TestActor, DateTime.UtcNow, null, TransactionType.ContractTransaction, true));


// Temporarily disabled.
// TODO: https://github.com/AElfProject/AElf/issues/338
            // Start processing
//            var js1 = ExpectMsg<JobExecutionStatus>();
//            Assert.Equal(JobExecutionStatus.RequestStatus.Running, js1.Status);

            // Return result
            var trace = ExpectMsg<TransactionTraceMessage>().TransactionTraces.FirstOrDefault();

                        
//            var t = _mock.GetBalanceTxn(_mock.SampleContractAddress1, from);
//            _mock.Worker1.Tell(new JobExecutionRequest(0, _mock.ChainId1, new List<Transaction>(){t}, TestActor, TestActor));
//            var tt = ExpectMsg<TransactionTraceMessage>().TransactionTraces;
//            Assert.Null(tt);

            

// Temporarily disabled.
// TODO: https://github.com/AElfProject/AElf/issues/338
            // Completed, two messages will be received
            // 1 for sender, 1 for router (in this test both are TestActor)
//            ExpectMsg<JobExecutionStatus>();
//            var js2 = ExpectMsg<JobExecutionStatus>();
//            Assert.Equal(JobExecutionStatus.RequestStatus.Completed, js2.Status);

            Assert.Equal(tx1.GetHash(), trace.TransactionId);
            if (!string.IsNullOrEmpty(trace.StdErr))
            {
                Assert.Null(trace.StdErr);
            }

            Assert.Equal((ulong) 90, _mock.GetBalance1(from));
            Assert.Equal((ulong) 10, _mock.GetBalance1(to));


// Temporarily disabled.
// TODO: https://github.com/AElfProject/AElf/issues/338
            // Query status
            _mock.Worker1.Tell(new JobExecutionStatusQuery(0));

            // Invalid request id as it has already completed
//            var js3 = ExpectMsg<JobExecutionStatus>();
//            Assert.Equal(JobExecutionStatus.RequestStatus.InvalidRequestId, js3.Status);

        }

        [Fact]
        public void MultipleTransactionExecutionTest()
        {
            var address1 = Address.Generate();
            var address2 = Address.Generate();
            var address3 = Address.Generate();
            var address4 = Address.Generate();
            var address5 = Address.Generate();
            var address6 = Address.Generate();
            
            _mock.Initialize1(address1, 100);
            _mock.Initialize1(address2, 0);
            _mock.Initialize1(address3, 200);
            _mock.Initialize1(address4, 0);
            _mock.Initialize1(address5, 300);
            _mock.Initialize1(address6, 0);

            var tx1 = _mock.GetTransferTxn1(address1, address2, 10);
            var tx2 = _mock.GetTransferTxn1(address3, address4, 10);
            var tx3 = _mock.GetTransferTxn1(address5, address6, 10);

            // Normal transfer
            var job1 = new List<Transaction>
            {
                tx1,
                tx2,
                tx3
            };

            _mock.Worker1.Tell(new JobExecutionRequest(0, _mock.ChainId1, job1, TestActor, TestActor, DateTime.UtcNow, null, TransactionType.ContractTransaction, true));

            // Start processing


// Temporarily disabled.
// TODO: https://github.com/AElfProject/AElf/issues/338
//            var js1 = ExpectMsg<JobExecutionStatus>();
//            Assert.Equal(JobExecutionStatus.RequestStatus.Running, js1.Status);

            // Return result
            var trace = ExpectMsg<TransactionTraceMessage>().TransactionTraces;
            var trace1 = trace[0];
            var trace2 = trace[1];
            var trace3 = trace[2];

//            Assert.Null(trace1);
//            _mock.CommitTrace(trace1).Wait();
//            _mock.CommitTrace(trace2).Wait();
//            _mock.CommitTrace(trace3).Wait();
            // Completed


// Temporarily disabled.
// TODO: https://github.com/AElfProject/AElf/issues/338
//            var js2 = ExpectMsg<JobExecutionStatus>();
//            Assert.Equal(JobExecutionStatus.RequestStatus.Completed, js2.Status);

            Assert.Equal(tx1.GetHash(), trace1.TransactionId);
            Assert.Equal(tx2.GetHash(), trace2.TransactionId);
            Assert.Equal(tx3.GetHash(), trace3.TransactionId);

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
            
            if (!string.IsNullOrEmpty(trace3.StdErr))
            {
                Assert.Null(trace3.StdErr);
            }
            Assert.Equal((ulong) 290, _mock.GetBalance1(address5));
            Assert.Equal((ulong) 10, _mock.GetBalance1(address6));
            
            // Check sequence
            var end1 = _mock.GetTransactionEndTime1(tx1);
            var start2 = _mock.GetTransactionStartTime1(tx2);
            Assert.True(end1 < start2);
        }

// Temporarily disabled.
// TODO: https://github.com/AElfProject/AElf/issues/338
//        [Fact]
//        public void JobCancelTest()
//        {
//            var job = new List<ITransaction>()
//            {
//                _mock.GetSleepTxn1(1000),
//                _mock.GetSleepTxn1(1000),
//                _mock.GetNoActionTxn1()
//            };
//
//            _mock.Worker1.Tell(new JobExecutionRequest(0, _mock.ChainId1, job, TestActor,
//                TestActor));
//
//            Thread.Sleep(1500);
//            _mock.Worker1.Tell(JobExecutionCancelMessage.Instance);
//
//            var traces = new List<TransactionTrace>()
//            {
//                ((TransactionTraceMessage) FishForMessage(msg => msg is TransactionTraceMessage))
//                .TransactionTrace,
//                ((TransactionTraceMessage) FishForMessage(msg => msg is TransactionTraceMessage))
//                .TransactionTrace,
//                ((TransactionTraceMessage) FishForMessage(msg => msg is TransactionTraceMessage))
//                .TransactionTrace
//            };
//
//            Assert.Equal("Execution Cancelled", traces[2].StdErr);
//        }
    }
    */
}