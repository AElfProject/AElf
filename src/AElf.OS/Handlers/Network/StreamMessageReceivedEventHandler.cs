using System.Threading.Tasks;
using AElf.OS.Network.Events;
using AElf.OS.Network.Grpc;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.OS.Handlers;

public class StreamMessageReceivedEventHandler : ILocalEventHandler<StreamMessageReceivedEvent>, ITransientDependency
{
    private readonly IStreamService _streamService;

    public StreamMessageReceivedEventHandler(IStreamService streamService)
    {
        _streamService = streamService;
    }

    public async Task HandleEventAsync(StreamMessageReceivedEvent eventData)
    {
        await _streamService.ProcessStreamReply(eventData.Message, eventData.ClientPubkey);
    }
}