using System;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AElf
{
    public class TaskQueueTests
    {
        private TaskQueue _taskQueue;
        private int _counter;
        public TaskQueueTests()
        {
            _taskQueue = new TaskQueue();
            _counter = 0;
        }

        [Fact]
        public async Task StartQueueTest_Twice()
        {
            _taskQueue.StartAsync();
            await Should.ThrowAsync<InvalidOperationException>(async () => await _taskQueue.StartAsync());
        }

        [Fact]
        public async Task EnqueueTest()
        {
            _taskQueue.StartAsync();
            _taskQueue.Enqueue(ProcessTask);
            Thread.Sleep(10);
            _counter.ShouldBe(1);
            
            _taskQueue.Dispose();
            Should.Throw<InvalidOperationException>(() => _taskQueue.Enqueue(ProcessTask));
        }

        [Fact]
        public async Task Enqueue_MultipleTimes()
        {
            _taskQueue.StartAsync();
            for (var i = 0; i < 10; i++)
            {
                _taskQueue.Enqueue(ProcessTask);
                Thread.Sleep(10);
            }
            _counter.ShouldBe(10);
        }

        [Fact]
        public async Task Dispose_QueueTest()
        {
            _taskQueue.StartAsync();
            _taskQueue.Dispose();

            Should.Throw<InvalidOperationException>(() => _taskQueue.Enqueue(ProcessTask));
        }

        [Fact]
        public async Task Stop_QueueTest()
        {
            _taskQueue.StartAsync();
            await _taskQueue.StopAsync();
            Should.Throw<InvalidOperationException>(() => _taskQueue.Enqueue(ProcessTask));
        }

        private async Task ProcessTask()
        {
            await Task.Run(() => _counter++);
        }
    }
}