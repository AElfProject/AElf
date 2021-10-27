using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AElf
{
    public class TaskQueueManagerTests : CoreAElfTestBase
    {
        private readonly ITaskQueueManager _taskQueueManager;

        public TaskQueueManagerTests()
        {
            _taskQueueManager = GetRequiredService<ITaskQueueManager>();
        }

        [Fact]
        public void Test_StartAsync_Test()
        {
            var testQueue = _taskQueueManager.CreateQueue("TestQueue");

            Should.Throw<InvalidOperationException>(()=> _taskQueueManager.CreateQueue("TestQueue"));
            
            Should.Throw<InvalidOperationException>(()=>testQueue.Start());

            testQueue.Dispose();
            Should.Throw<InvalidOperationException>(() => testQueue.Enqueue(() => Task.CompletedTask));
        }

        [Fact]
        public void Test_Enqueue_Test()
        {
            var result = 1;
            var testQueue = _taskQueueManager.CreateQueue("TestQueue");
            Parallel.For(0, 100, i =>
            {
                testQueue.Enqueue(() =>
                {
                    var value = result;
                    result = value + 1;
                    return Task.CompletedTask;
                });
            });

            testQueue.Dispose();

            result.ShouldBe(101);
        }

        [Fact]
        public void Test_Many_Enqueue_Test()
        {
            var testData = new int[3];
            var testQueueA = _taskQueueManager.CreateQueue("TestQueueA");
            var testQueueB = _taskQueueManager.CreateQueue("TestQueueB");
            var testQueueC = _taskQueueManager.CreateQueue("TestQueueC");

            Parallel.For(0, 100, i =>
            {
                testQueueA.Enqueue(() => { testData[0]++; return Task.CompletedTask;});
                testQueueB.Enqueue(() => { testData[1]++; return Task.CompletedTask;});
                testQueueC.Enqueue(() => { testData[2]++; return Task.CompletedTask;});
            });

            testQueueA.Dispose();
            testQueueB.Dispose();
            testQueueC.Dispose();

            testData[0].ShouldBe(100);
            testData[1].ShouldBe(100);
            testData[2].ShouldBe(100);
        }

        [Fact]
        public void Test_Dispose_Test()
        {
            var result = 1;

            var testQueue = _taskQueueManager.CreateQueue("TestQueue");

            Parallel.For(0, 3, i =>
            {
                testQueue.Enqueue(async () =>
                {
                    var value = result;
                    await Task.Delay(100);
                    result = value + 1;
                });
            });
            testQueue.Dispose();

            result.ShouldBe(4);

            Should.Throw<InvalidOperationException>(() => testQueue.Enqueue(() => { result++; return Task.CompletedTask;}));
        }

        [Fact]
        public void Test_GetQueue_Test()
        {
            var defaultQueueA = _taskQueueManager.GetQueue();
            var defaultQueueB = _taskQueueManager.GetQueue();
            defaultQueueA.ShouldBe(defaultQueueB);

            var testQueueA1 = _taskQueueManager.CreateQueue("TestQueueA");
            var testQueueA2 = _taskQueueManager.GetQueue("TestQueueA");
            testQueueA1.ShouldBe(testQueueA2);
            testQueueA1.ShouldNotBe(defaultQueueA);

            var testQueueB1 = _taskQueueManager.CreateQueue("TestQueueB");
            var testQueueB2 = _taskQueueManager.GetQueue("TestQueueB");
            testQueueB1.ShouldBe(testQueueB2);
            testQueueB1.ShouldNotBe(defaultQueueA);
            testQueueB1.ShouldNotBe(testQueueA1);
            
            _taskQueueManager.Dispose();
        }

        [Fact]
        public void Test_TaskQueue_StopAsync_Test()
        {
            var testQueue = _taskQueueManager.CreateQueue("TestQueue");
            testQueue.Dispose();

            var result = 1;
            Should.Throw<InvalidOperationException>(() =>
                testQueue.Enqueue(() =>
                {
                    var value = result;
                    result = value + 1;
                    return Task.CompletedTask;
                })
            );
        }

        [Fact]
        public void GetQueueStatus_Test()
        {
            _taskQueueManager.CreateQueue("TestQueueA");
            _taskQueueManager.CreateQueue("TestQueueB");

            var queueInfos = _taskQueueManager.GetQueueStatus();
            queueInfos.Count.ShouldBe(2);
            queueInfos.Select(o=>o.Name).ShouldContain("TestQueueA");            
            queueInfos.Select(o=>o.Name).ShouldContain("TestQueueB");
            
            _taskQueueManager.Dispose();
        }
    }
}