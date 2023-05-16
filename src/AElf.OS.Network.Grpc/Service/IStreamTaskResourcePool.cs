using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Grpc;

public interface IStreamTaskResourcePool
{
    Task RegistryTaskPromiseAsync(string requestId, MessageType messageType, TaskCompletionSource<StreamMessage> promise);
    void TrySetResult(string requestId, StreamMessage reply);
    Task<StreamMessage> GetResultAsync(TaskCompletionSource<StreamMessage> promise, string requestId, int timeOut);
}

public class StreamTaskResourcePool : IStreamTaskResourcePool, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, Tuple<MessageType, TaskCompletionSource<StreamMessage>>> _promisePool;

    public StreamTaskResourcePool()
    {
        _promisePool = new ConcurrentDictionary<string, Tuple<MessageType, TaskCompletionSource<StreamMessage>>>();
    }

    public Task RegistryTaskPromiseAsync(string requestId, MessageType messageType, TaskCompletionSource<StreamMessage> promise)
    {
        _promisePool[requestId] = new Tuple<MessageType, TaskCompletionSource<StreamMessage>>(messageType, promise);
        return Task.CompletedTask;
    }

    public void TrySetResult(string requestId, StreamMessage reply)
    {
        AssertContains(requestId);
        var promise = _promisePool[requestId];
        if (promise.Item1 != reply.MessageType)
        {
            throw new Exception($"invalid reply type set {reply.StreamType} expect {promise.Item1}");
        }

        promise.Item2.TrySetResult(reply);
    }

    public async Task<StreamMessage> GetResultAsync(TaskCompletionSource<StreamMessage> promise, string requestId, int timeOut)
    {
        try
        {
            var completed = await Task.WhenAny(promise.Task, Task.Delay(timeOut));
            if (completed != promise.Task)
                throw new TimeoutException($"streaming call time out requestId {requestId}");

            var message = await promise.Task;
            return message;
        }
        finally
        {
            _promisePool.TryRemove(requestId, out _);
        }
    }

    private void AssertContains(string requestId)
    {
        if (!_promisePool.ContainsKey(requestId))
        {
            throw new Exception($"{requestId} not found");
        }
    }
}