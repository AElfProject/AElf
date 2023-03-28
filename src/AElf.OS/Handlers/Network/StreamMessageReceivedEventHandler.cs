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
    public ILogger<AbnormalPeerEventHandler> Logger { get; set; }

    public StreamMessageReceivedEventHandler(IStreamService streamService)
    {
        _streamService = streamService;
    }

    public async Task HandleEventAsync(StreamMessageReceivedEvent eventData)
    {
        var message = StreamMessage.Parser.ParseFrom(eventData.Message);
        Logger.LogInformation("handle event requestId {requestId} streamType={streamType} {pubkey}", message.RequestId, message.StreamType, eventData.ClientPubkey);
        await _streamService.ProcessStreamReply(eventData.Message, eventData.ClientPubkey);
    }
}