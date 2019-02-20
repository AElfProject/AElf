//using System;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Collections.Generic;
//using Xunit;
//using Akka.Actor;
//using Akka.TestKit;
//using Akka.TestKit.Xunit;
//using AElf.Kernel.SmartContractExecution;
//using AElf.Kernel.SmartContractExecution.Execution;

namespace AElf.Kernel.Tests.Concurrency.Execution
{
 /*
 TODO: https://github.com/AElfProject/AElf/issues/338 
    public class TrackedRouterTest : TestKitBase
    {
        private MockSetup _mock;

        public TrackedRouterTest(MockSetup mock) : base(new XunitAssertions())
        {
            _mock = mock;
        }

        [Fact(Skip = "Ignore for now")]
        public void ThreeJobsExecutionTest()
        {
            // As there are only two workers, the third job will fail
            var job = new List<Transaction>() {_mock.GetSleepTxn1(1000)};

            _mock.Router.Tell(new JobExecutionRequest(0, _mock.ChainId1, job, TestActor, _mock.Router, DateTime.UtcNow));
            _mock.Router.Tell(new JobExecutionRequest(1, _mock.ChainId1, job, TestActor, _mock.Router, DateTime.UtcNow));
            _mock.Router.Tell(new JobExecutionRequest(2, _mock.ChainId1, job, TestActor, _mock.Router, DateTime.UtcNow));

            // The third job fails
            FishForMessage(
                msg =>
                    msg is JobExecutionStatus js &&
                    js.RequestId == 2 &&
                    js.Status == JobExecutionStatus.RequestStatus.FailedDueToNoAvailableWorker
            );
            
            FishForMessage(
                msg =>
                    msg is JobExecutionStatus js &&
                    js.Status == JobExecutionStatus.RequestStatus.Completed
            );

            FishForMessage(
                msg =>
                    msg is JobExecutionStatus js &&
                    js.Status == JobExecutionStatus.RequestStatus.Completed
            );

        }
        
        [Fact(Skip = "Ignore for now")]
        public void WorkerBecomeIdleExecutionTest()
        {
            // As there are only two workers, the third job will fail
            var job = new List<Transaction>() {_mock.GetSleepTxn1(1000)};

            _mock.Router.Tell(new JobExecutionRequest(0, _mock.ChainId1, job, TestActor, _mock.Router, DateTime.UtcNow));
            _mock.Router.Tell(new JobExecutionRequest(1, _mock.ChainId1, job, TestActor, _mock.Router, DateTime.UtcNow));
            
            FishForMessage(
                (msg) =>
                    msg is JobExecutionStatus js &&
                    js.Status == JobExecutionStatus.RequestStatus.Completed
            );

            FishForMessage(
                (msg) =>
                    msg is JobExecutionStatus js &&
                    js.Status == JobExecutionStatus.RequestStatus.Completed
            );

            _mock.Router.Tell(new JobExecutionRequest(2, _mock.ChainId1, job, TestActor, _mock.Router, DateTime.UtcNow));

            FishForMessage(
                (msg) =>
                    msg is JobExecutionStatus js &&
                    js.RequestId == 2 &&
                    js.Status == JobExecutionStatus.RequestStatus.Running,
                TimeSpan.FromSeconds(10)
            );
        }
    }
*/
}