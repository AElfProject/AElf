using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AElf.WebApp.MessageQueue;

public interface ISendMessageByDesignateHeightTaskManager
{
    void Start(long height);
    Task StopAsync();
}

public class SendMessageByDesignateHeightTaskManager : ISendMessageByDesignateHeightTaskManager, ISingletonDependency
{
    private readonly SendMessageWorker _sendMessageWorker;
    private Task _task;
    private CancellationTokenSource _cancellationTokenSource = null;

    public SendMessageByDesignateHeightTaskManager(SendMessageWorker sendMessageWorker)
    {
        _sendMessageWorker = sendMessageWorker;
    }

    public void Start(long height)
    {
        if (_cancellationTokenSource != null)
        {
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _task = _sendMessageWorker.StartAsync(height, _cancellationTokenSource.Token);
    }

    public async Task StopAsync()
    {
        if (_cancellationTokenSource == null)
        {
            return;
        }

        _cancellationTokenSource.Cancel();
        await _task;
        _cancellationTokenSource = null;
    }
}