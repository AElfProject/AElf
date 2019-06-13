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
        int Size { get; }

        int MaxDegreeOfParallelism { get; }

        void Start(int maxDegreeOfParallelism = 1);
    }

    public class TaskQueue : ITaskQueue, ITransientDependency
    {
        private ActionBlock<Func<Task>> _actionBlock;

        public ILogger<TaskQueue> Logger { get; set; }

        public int Size => _actionBlock.InputCount;
        public int MaxDegreeOfParallelism { get; private set; } = 1;

        public void Start(int maxDegreeOfParallelism = 1)
        {
            if (_actionBlock != null)
                throw new InvalidOperationException("already started");

            MaxDegreeOfParallelism = maxDegreeOfParallelism;

            _actionBlock = new ActionBlock<Func<Task>>(async func =>
            {
                try
                {
                    await func();
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, LogLevel.Warning);
                }
            }, new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = MaxDegreeOfParallelism
            });
        }

        public TaskQueue()
        {
            Logger = NullLogger<TaskQueue>.Instance;
        }

        public void Dispose()
        {
            _actionBlock.Complete();

            _actionBlock.Completion.Wait();
        }

        public void Enqueue(Func<Task> task)
        {
            if (!_actionBlock.Post(task))
                throw new InvalidOperationException("unable to enqueue a task");
        }
    }


    public interface ITaskQueueManager : IDisposable
    {
        ITaskQueue GetQueue(string name = null);

        ITaskQueue CreateQueue(string name, int maxDegreeOfParallelism = 1);

        List<TaskQueueInfo> GetQueueStatus();
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

        private readonly ITaskQueue _defaultTaskQueue;

        private readonly ConcurrentDictionary<string, ITaskQueue> _taskQueues =
            new ConcurrentDictionary<string, ITaskQueue>();

        public TaskQueueManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            _defaultTaskQueue = _serviceProvider.GetService<ITaskQueue>();
            _defaultTaskQueue.Start();
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

            _taskQueues.TryGetValue(name, out var queue);
            return queue;
        }

        public ITaskQueue CreateQueue(string name, int maxDegreeOfParallelism)
        {
            var q = _serviceProvider.GetService<ITaskQueue>();
            q.Start(maxDegreeOfParallelism);

            if (!_taskQueues.TryAdd(name, q))
            {
                throw new InvalidOperationException("queue already created");
            }

            return q;
        }

        public List<TaskQueueInfo> GetQueueStatus()
        {
            var result = new List<TaskQueueInfo>();
            foreach (var taskQueueName in _taskQueues.Keys)
            {
                _taskQueues.TryGetValue(taskQueueName, out var queue);
                result.Add(new TaskQueueInfo
                {
                    Name = taskQueueName,
                    Size = queue?.Size ?? 0
                });
            }

            return result;
        }
    }

    public class TaskQueueInfo
    {
        public string Name { get; set; }

        public int Size { get; set; }
    }
}