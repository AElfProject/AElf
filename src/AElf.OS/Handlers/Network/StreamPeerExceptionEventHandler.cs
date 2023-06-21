using System;
using System.Security.AccessControl;
using System.Threading.Tasks;
using AElf.OS.Network.Events;
using AElf.OS.Network.Grpc;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers;

public class StreamPeerExceptionEventHandler : ILocalEventHandler<StreamPeerExceptionEvent>, ITransientDependency
{
    private readonly IStreamService _streamService;
    public ILogger<AnnouncementReceivedEventHandler> Logger { get; set; }

    public StreamPeerExceptionEventHandler(IStreamService streamService)
    {
        _streamService = streamService;
    }

    public async Task HandleEventAsync(StreamPeerExceptionEvent eventData)
    {
        await _streamService.ProcessStreamPeerException(eventData.Exception, eventData.Peer);
    }
}