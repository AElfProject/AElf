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

    public Task HandleEventAsync(StreamMessageReceivedEvent eventData)
    {
        //because our message do not have relation between each other, so we want it to be processed concurrency
        _streamService.ProcessStreamReplyAsync(eventData.Message, eventData.ClientPubkey);
        return Task.CompletedTask;
    }
}