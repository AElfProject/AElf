using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using Xunit.Frameworks.Autofac;
using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Execution.Messages;
using StackExchange.Redis;

namespace AElf.Kernel.Tests.Concurrency.Execution
{
    [UseAutofacTestFramework]
    public class TrackedRouterTest : TestKitBase
    {
        private MockSetup _mock;
        private ActorSystem sys = ActorSystem.Create("test");
        private IActorRef _router;

        public TrackedRouterTest(MockSetup mock) : base(new XunitAssertions())
        {
            _mock = mock;

            var workers = new[] {"/user/worker1", "/user/worker2"};
            var worker1 = sys.ActorOf(Props.Create<Worker>(), "worker1");
            var worker2 = sys.ActorOf(Props.Create<Worker>(), "worker2");
            _router = sys.ActorOf(Props.Empty.WithRouter(new TrackedGroup(workers)), "router");
            worker1.Tell(new LocalSerivcePack(_mock.ServicePack));
            worker2.Tell(new LocalSerivcePack(_mock.ServicePack));
        }

        [Fact]
        public void ThreeJobsExecutionTest()
        {
            // As there are only two workers, the third job will fail
            var job = new List<ITransaction>() {_mock.GetSleepTxn1(1000)};

            _router.Tell(new JobExecutionRequest(0, _mock.ChainId1, job, TestActor, null));
            _router.Tell(new JobExecutionRequest(1, _mock.ChainId1, job, TestActor, null));
            _router.Tell(new JobExecutionRequest(2, _mock.ChainId1, job, TestActor, null));

            // The third job fails
            FishForMessage(
                (msg) =>
                    msg is JobExecutionStatus js &&
                    js.RequestId == 2 &&
                    js.Status == JobExecutionStatus.RequestStatus.FailedDueToNoAvailableWorker
            );
            
            FishForMessage(
                (msg) =>
                    msg is JobExecutionStatus js &&
                    js.RequestId == 0 &&
                    js.Status == JobExecutionStatus.RequestStatus.Completed
            );

            FishForMessage(
                (msg) =>
                    msg is JobExecutionStatus js &&
                    js.RequestId == 1 &&
                    js.Status == JobExecutionStatus.RequestStatus.Completed
            );

        }
    }
}