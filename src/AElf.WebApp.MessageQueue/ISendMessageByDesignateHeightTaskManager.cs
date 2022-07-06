using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AElf.WebApp.MessageQueue;

public interface ISendMessageByDesignateHeightTaskManager
{
    Task StartAsync();
    Task StopAsync();
}

public class SendMessageByDesignateHeightTaskManager : ISendMessageByDesignateHeightTaskManager, ISingletonDependency
{
    private readonly SendMessageWorker _sendMessageWorker;
    private CancellationTokenSource _cancellationTokenSource = null;

    public SendMessageByDesignateHeightTaskManager(SendMessageWorker sendMessageWorker)
    {
        _sendMessageWorker = sendMessageWorker;
    }

    public async Task StartAsync()
    {
        if (_cancellationTokenSource != null)
        {
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        await _sendMessageWorker.StartAsync(_cancellationTokenSource.Token);
    }

    public async Task StopAsync()
    {
        if (_cancellationTokenSource == null)
        {
            return;
        }
        
        _cancellationTokenSource.Cancel();
        await _sendMessageWorker.StopAsync();
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = null;
    }
}