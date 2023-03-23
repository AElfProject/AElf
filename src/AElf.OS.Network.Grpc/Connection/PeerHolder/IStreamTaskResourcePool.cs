using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Grpc;

public interface IStreamTaskResourcePool
{
    void RegistryTaskPromise(string requestId, StreamType streamType, TaskCompletionSource<StreamMessage> promise);
    void TrySetResult(string requestId, StreamMessage reply);
    Task<StreamMessage> GetResult(string requestId, int timeOut);
}

public class StreamTaskResourcePool : IStreamTaskResourcePool, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, Tuple<StreamType, TaskCompletionSource<StreamMessage>>> _promisePool;

    public StreamTaskResourcePool()
    {
        _promisePool = new ConcurrentDictionary<string, Tuple<StreamType, TaskCompletionSource<StreamMessage>>>();
    }

    public void RegistryTaskPromise(string requestId, StreamType streamType, TaskCompletionSource<StreamMessage> promise)
    {
        _promisePool[requestId] = new Tuple<StreamType, TaskCompletionSource<StreamMessage>>(streamType, promise);
    }

    public void TrySetResult(string requestId, StreamMessage reply)
    {
        AssertContains(requestId);
        var promise = _promisePool[requestId];
        if (promise.Item1 != reply.StreamType)
        {
            throw new Exception($"invalid reply type set {reply.StreamType} expect {promise.Item1}");
        }

        promise.Item2.TrySetResult(reply);
    }

    public async Task<StreamMessage> GetResult(string requestId, int timeOut)
    {
        AssertContains(requestId);
        var promise = _promisePool[requestId].Item2;
        var completed = await Task.WhenAny(promise.Task, Task.Delay(timeOut));
        if (completed == promise.Task)
        {
            return await promise.Task;
        }

        throw new TimeoutException($"streaming call time out requestId {requestId}");
    }

    private void AssertContains(string requestId)
    {
        if (!_promisePool.ContainsKey(requestId))
        {
            throw new Exception($"{requestId} not found");
        }
    }
}