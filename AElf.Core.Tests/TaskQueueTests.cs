using System;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AElf
{
    public class TaskQueueTests : CoreAElfTestBase
    {
        private readonly ITaskQueueManager _taskQueueManager;
        
        public TaskQueueTests()
        {
            _taskQueueManager = GetRequiredService<ITaskQueueManager>();
        }
        
        [Fact]
        public void Test_StartAsync()
        {
            var testQueue = _taskQueueManager.GetQueue("TestQueue");
            testQueue.StartAsync().ShouldThrow<InvalidOperationException>();

            testQueue.StopAsync();
        }

        [Fact]
        public void Test_Enqueue()
        {
            var result = 1;
            var testQueue = _taskQueueManager.GetQueue("TestQueue");
            Parallel.For(0, 100, i =>
            {
                testQueue.Enqueue(() =>
                {
                    var value = result;
                    Thread.Sleep(10);
                    result = value +1;
                    return null;
                });
            });
            
            Thread.Sleep(2000);
            result.ShouldBe(101);
        }

        [Fact]
        public void Test_Dispose()
        {
            var result = 1;
            
            var testQueue = _taskQueueManager.GetQueue("TestQueue");

            Parallel.For(0, 3, i =>
            {
                testQueue.Enqueue(() =>
                {
                    var value = result;
                    Thread.Sleep(1000);
                    result = value +1;
                    return null;
                });
            });
            testQueue.Dispose();
            
            Thread.Sleep(2000);
            result.ShouldBe(2);
            
            testQueue.Enqueue(() =>
            {
                result++;
                return null;
            });
            
            Thread.Sleep(2000);
            result.ShouldBe(2);
        }
        
        [Fact]
        public void Test_GetQueue()
        {
            var defaultQueueA = _taskQueueManager.GetQueue();
            var defaultQueueB = _taskQueueManager.GetQueue();
            defaultQueueA.ShouldBe(defaultQueueB);

            var testQueueA1 = _taskQueueManager.GetQueue("TestQueueA");
            var testQueueA2 = _taskQueueManager.GetQueue("TestQueueA");
            testQueueA1.ShouldBe(testQueueA2);
            testQueueA1.ShouldNotBe(defaultQueueA);
            
            var testQueueB1 = _taskQueueManager.GetQueue("TestQueueB");
            var testQueueB2 = _taskQueueManager.GetQueue("TestQueueB");
            testQueueB1.ShouldBe(testQueueB2);
            testQueueB1.ShouldNotBe(defaultQueueA);
            testQueueB1.ShouldNotBe(testQueueA1);
        }
    }
}