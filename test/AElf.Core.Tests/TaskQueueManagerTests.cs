using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute.ExceptionExtensions;
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
        public void Test_StartAsync()
        {
            var testQueue = _taskQueueManager.CreateQueue("TestQueue");
            
            Should.Throw<InvalidOperationException>(()=>testQueue.Start());

            testQueue.Dispose();
            Should.Throw<InvalidOperationException>(() => testQueue.Enqueue(async () => { }));
        }

        [Fact]
        public async Task Test_Enqueue()
        {
            var result = 1;
            var testQueue = _taskQueueManager.CreateQueue("TestQueue");
            Parallel.For(0, 100, i =>
            {
                testQueue.Enqueue(async () =>
                {
                    var value = result;
                    result = value + 1;
                });
            });

            testQueue.Dispose();

            result.ShouldBe(101);
        }

        [Fact]
        public async Task Test_Many_Enqueue()
        {
            var testData = new int[3];
            var testQueueA = _taskQueueManager.CreateQueue("TestQueueA");
            var testQueueB = _taskQueueManager.CreateQueue("TestQueueB");
            var testQueueC = _taskQueueManager.CreateQueue("TestQueueC");

            Parallel.For(0, 100, i =>
            {
                testQueueA.Enqueue(async () => { testData[0]++; });
                testQueueB.Enqueue(async () => { testData[1]++; });
                testQueueC.Enqueue(async () => { testData[2]++; });
            });

            testQueueA.Dispose();
            testQueueB.Dispose();
            testQueueC.Dispose();

            testData[0].ShouldBe(100);
            testData[1].ShouldBe(100);
            testData[2].ShouldBe(100);
        }

        [Fact]
        public async Task Test_Dispose()
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

            Should.Throw<InvalidOperationException>(() => testQueue.Enqueue(async () => { result++; }));
        }

        [Fact]
        public void Test_GetQueue()
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
        }

        [Fact]
        public async Task Test_TaskQueue_StopAsync()
        {
            var testQueue = _taskQueueManager.CreateQueue("TestQueue");
            testQueue.Dispose();

            var result = 1;
            Should.Throw<InvalidOperationException>(() =>
                testQueue.Enqueue(async () =>
                {
                    var value = result;
                    result = value + 1;
                })
            );
        }
    }
}