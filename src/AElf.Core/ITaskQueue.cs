using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf
{
    public interface ITaskQueue : IDisposable
    {
        void Enqueue(Func<Task> task);
        Task StartAsync();
        Task StopAsync();
        int Size { get; }
    }

    public class TaskQueue : ITaskQueue, ITransientDependency
    {
        private BufferBlock<Func<Task>> _queue = new BufferBlock<Func<Task>>();

        private CancellationTokenSource _cancellationTokenSource;

        public ILogger<TaskQueue> Logger { get; set; }

        public int Size => _queue.Count;

        public TaskQueue()
        {
            Logger = NullLogger<TaskQueue>.Instance;
        }

        public void Dispose()
        {
            if (_cancellationTokenSource != null)
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                }

                if (_queue.Count > 0)
                    Task.WaitAny(_queue.Completion);
                _cancellationTokenSource.Dispose();
            }
        }

        public void Enqueue(Func<Task> task)
        {
            if (_cancellationTokenSource.Token.IsCancellationRequested)
                throw new InvalidOperationException("cannot enqueue into a stopped queue");
            _queue.Post(task);
        }

        public async Task StartAsync()
        {
            if (_cancellationTokenSource != null)
                throw new InvalidOperationException("Already started");

            _cancellationTokenSource = new CancellationTokenSource();

            while (await _queue.OutputAvailableAsync())
            {
                try
                {
                    var func = await _queue.ReceiveAsync();
                    await func();
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, LogLevel.Warning);
                }

                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    if (_queue.Count == 0)
                        _queue.Complete();
                }
            }
        }

        public async Task StopAsync()
        {
            _cancellationTokenSource.Cancel();
        }
    }


    public interface ITaskQueueManager : IDisposable
    {
        ITaskQueue GetQueue(string name = null);
        List<TaskQueueStateInfo> GetQueueStateInfos();
    }

    public static class TaskQueueManagerExtensions
    {
        public static void Enqueue(this ITaskQueueManager taskQueueManager, Func<Task> task, string name = null)
        {
            taskQueueManager.GetQueue(name).Enqueue(task);
        }
    }

    public class TaskQueueManager : ITaskQueueManager, ISingletonDependency
    {
        private readonly IServiceProvider _serviceProvider;

        private ITaskQueue _defaultTaskQueue;

        private ConcurrentDictionary<string, ITaskQueue> _taskQueues = new ConcurrentDictionary<string, ITaskQueue>();

        public TaskQueueManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            _defaultTaskQueue = _serviceProvider.GetService<ITaskQueue>();

            AsyncHelper.RunSync(() =>
                Task.Factory.StartNew(() => _defaultTaskQueue.StartAsync(), TaskCreationOptions.LongRunning));
        }

        public void Dispose()
        {
            _defaultTaskQueue.Dispose();
            foreach (var taskQueue in _taskQueues.Values)
            {
                taskQueue.Dispose();
            }
        }

        public ITaskQueue GetQueue(string name = null)
        {
            if (name == null)
                return _defaultTaskQueue;

            if (_taskQueues.TryGetValue(name, out var queue))
            {
                return queue;
            }

            return _taskQueues.GetOrAdd(name, _ =>
            {
                var q = _serviceProvider.GetService<ITaskQueue>();
                AsyncHelper.RunSync(() => Task.Factory.StartNew(() => q.StartAsync(), TaskCreationOptions
                    .LongRunning));
                return q;
            });
        }

        public List<TaskQueueStateInfo> GetQueueStateInfos()
        {
            var result = new List<TaskQueueStateInfo>();
            foreach (var taskQueueName in _taskQueues.Keys)
            {
                _taskQueues.TryGetValue(taskQueueName, out var queue);
                result.Add(new TaskQueueStateInfo
                {
                    Name = taskQueueName,
                    Size = queue?.Size ?? 0
                });
            }

            return result;
        }
    }

    public class TaskQueueStateInfo
    {
        public string Name { get; set; }

        public int Size { get; set; }
    }
}