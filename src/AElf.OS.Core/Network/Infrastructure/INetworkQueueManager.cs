using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Infrastructure
{
    public interface INetworkQueueManager : IDisposable
    {
        void CreateQueue(string name, int consumerCount);
        INetworkQueue GetQueue(string name);
    }

    public interface INetworkQueue : IDisposable
    {
        void Start(int consumerCount);
        Task EnqueueAsync(Func<Task> task);
    }

    public class NetworkQueue : INetworkQueue, ITransientDependency
    {
        public ILogger<TaskQueue> Logger { get; set; }
        
        private readonly AsyncProducerConsumerQueue<Func<Task>> _queue;

        public NetworkQueue()
        {
            _queue = new AsyncProducerConsumerQueue<Func<Task>>();
        }

        public void Start(int consumerCount = 1)
        {
            for (int i = 0; i < consumerCount; i++)
                StartConsumer();
        }

        private void StartConsumer()
        {
            // todo add cancellation
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    var func = await _queue.DequeueAsync();
                    try
                    {
                        await func();
                    }
                    catch (Exception e)
                    {
                        Logger.LogDebug(e, "Error while dequeuing.");
                    }
                }
            });
        }

        public async Task EnqueueAsync(Func<Task> task)
        {
            await _queue.EnqueueAsync(task);
        }
        
        public void Dispose()
        {
            // todo
        }
    }

    public class NetworkQueueManager : INetworkQueueManager, ISingletonDependency
    {
        private readonly IServiceProvider _serviceProvider;

        private ConcurrentDictionary<string, INetworkQueue> _taskQueues = new ConcurrentDictionary<string, INetworkQueue>();

        public NetworkQueueManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void CreateQueue(string name, int consumerCount)
        {
            if (_taskQueues.TryGetValue(name, out _))
                throw new InvalidOperationException($"A queue named {name} already exists.");
            
            var newQueue = _serviceProvider.GetService<INetworkQueue>();

            if (!_taskQueues.TryAdd(name, newQueue))
                throw new InvalidOperationException($"A queue named {name} already exists.");

            newQueue.Start(consumerCount);
        }

        public INetworkQueue GetQueue(string name)
        {
            if (!_taskQueues.TryGetValue(name, out var queue))
                throw new InvalidOperationException($"No queue named {name} exists.");

            return queue;
        }

        public void Dispose()
        {
            foreach (var taskQueue in _taskQueues.Values)
            {
                taskQueue.Dispose();
            }
        }
    }
}