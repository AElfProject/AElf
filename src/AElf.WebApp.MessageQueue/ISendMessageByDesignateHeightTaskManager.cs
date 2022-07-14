using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AElf.WebApp.MessageQueue;

public interface ISendMessageByDesignateHeightTaskManager
{
    Task StartAsync();
    Task StopAsync(bool stopWorker = false);

    void SetWorker(int? period, int? blockCountPerPeriod, int? parallelCount);
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

    public async Task StopAsync(bool stopWorker = false)
    {
        if (_cancellationTokenSource == null)
        {
            return;
        }

        _cancellationTokenSource.Cancel();
        if (stopWorker)
        {
            await _sendMessageWorker.StopAsync();
        }
        else
        {
            await _sendMessageWorker.StopTimerAsync();
        }
       
        _cancellationTokenSource.Dispose();
        _cancellationTokenSource = null;
    }

    public void SetWorker(int? period, int? blockCountPerPeriod, int? parallelCount)
    {
        _sendMessageWorker.SetWork(period, blockCountPerPeriod, parallelCount);
    }
}