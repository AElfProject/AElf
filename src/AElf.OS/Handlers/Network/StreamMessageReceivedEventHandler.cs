using System;
using System.Threading.Tasks;
using AElf.OS.Network.Events;
using AElf.OS.Network.Grpc;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers;

public class StreamMessageReceivedEventHandler : ILocalEventHandler<StreamMessageReceivedEvent>, ITransientDependency
{
    private readonly IStreamService _streamService;

    public ILogger<StreamMessageReceivedEventHandler> Logger { get; set; }

    public StreamMessageReceivedEventHandler(IStreamService streamService)
    {
        _streamService = streamService;
    }

    public Task HandleEventAsync(StreamMessageReceivedEvent eventData)
    {
        //because our message do not have relation between each other, so we want it to be processed concurrency
        if (eventData.RequestId != null)
            Logger.LogDebug("handleReceive {requestId} latency={latency}", eventData.RequestId, GetLatency(eventData.RequestId));
        _streamService.ProcessStreamReplyAsync(eventData.Message, eventData.ClientPubkey);
        return Task.CompletedTask;
    }

    private long GetLatency(string requestId)
    {
        var sp = requestId.Split("_");
        if (sp.Length != 2) return -1;
        return long.TryParse(sp[0], out var start) ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - start : -1;
    }
}