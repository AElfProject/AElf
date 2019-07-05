using System;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AElf
{
    public class TaskQueueTests
    {
        private readonly TaskQueue _taskQueue;
        private int _counter;
        public TaskQueueTests()
        {
            _taskQueue = new TaskQueue();
            _counter = 0;
        }

        [Fact]
        public void StartQueueTest_Twice()
        {
            _taskQueue.Start();
            Should.Throw<InvalidOperationException>(() => _taskQueue.Start());
        }

        [Fact]
        public void EnqueueTest()
        {
            _taskQueue.Start();
            _taskQueue.Enqueue(ProcessTask);
            Thread.Sleep(10);
            _counter.ShouldBe(1);
            
            _taskQueue.Dispose();
            Should.Throw<InvalidOperationException>(() => _taskQueue.Enqueue(ProcessTask));
        }

        [Fact]
        public void Enqueue_MultipleTimes()
        {
            _taskQueue.Start();
            for (var i = 0; i < 10; i++)
                _taskQueue.Enqueue(ProcessTask);

           _taskQueue.Dispose();
            _counter.ShouldBe(10);
        }

        [Fact]
        public void Dispose_QueueTest()
        {
            _taskQueue.Start();
            _taskQueue.Dispose();

            Should.Throw<InvalidOperationException>(() => _taskQueue.Enqueue(ProcessTask));
        }

        [Fact]
        public void Stop_QueueTest()
        {
            _taskQueue.Start();
            _taskQueue.Dispose();
            Should.Throw<InvalidOperationException>(() => _taskQueue.Enqueue(ProcessTask));
        }

        private async Task ProcessTask()
        {
            await Task.Run(() => _counter++);
        }
    }
}