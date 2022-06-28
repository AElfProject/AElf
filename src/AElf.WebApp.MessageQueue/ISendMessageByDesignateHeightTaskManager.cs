using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
    private ILogger<SendMessageByDesignateHeightTaskManager> _logger;

    public SendMessageByDesignateHeightTaskManager(SendMessageWorker sendMessageWorker, ILogger<SendMessageByDesignateHeightTaskManager> logger)
    {
        _sendMessageWorker = sendMessageWorker;
        _logger = logger;
    }

    public void Start(long height)
    {
        if (_cancellationTokenSource != null)
        {
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _task = _sendMessageWorker.StartAsync(height, _cancellationTokenSource.Token);
        _logger.LogInformation($"SendMessageWorker start to work, start height: {height}");
    }

    public async Task StopAsync()
    {
        if (_cancellationTokenSource == null)
        {
            return;
        }

        _cancellationTokenSource.Cancel();
        await _task;
        _logger.LogInformation("SendMessageWorker stop working");
        _cancellationTokenSource = null;
    }
}