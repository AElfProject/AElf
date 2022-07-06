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
    private bool _isStart;

    public SendMessageByDesignateHeightTaskManager(SendMessageWorker sendMessageWorker)
    {
        _sendMessageWorker = sendMessageWorker;
    }

    public async Task StartAsync()
    {
        if (_isStart)
        {
            return;
        }
        
        await _sendMessageWorker.StartAsync();
    }

    public async Task StopAsync()
    {
        if (!_isStart)
        {
            return;
        }
        
        await _sendMessageWorker.StopAsync();
        _isStart = false;
    }
}