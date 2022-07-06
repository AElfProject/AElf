using System.Threading;
using System.Threading.Tasks;
using AElf.WebApp.MessageQueue.Enum;
using AElf.WebApp.MessageQueue.Provider;
using AElf.WebApp.MessageQueue.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElf.WebApp.MessageQueue;

public class SendMessageWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly ISyncBlockStateProvider _syncBlockStateProvider;
    protected CancellationToken CancellationToken { get; set; }
    private readonly int _blockCount;

    public SendMessageWorker(ISyncBlockStateProvider syncBlockStateProvider, AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory, IOptionsSnapshot<MessageQueueOptions> option) : base(timer,
        serviceScopeFactory)
    {
        _syncBlockStateProvider = syncBlockStateProvider;
        _blockCount = option.Value.BlockCountPerPeriod;
        Timer.Period = option.Value.Period;
        timer.RunOnStart = true;
    }

    public override async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await base.StartAsync(cancellationToken);
        CancellationToken = cancellationToken;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var blockMessageService = workerContext.ServiceProvider.GetRequiredService<IBlockMessageService>();
        var currentState = await _syncBlockStateProvider.GetCurrentStateAsync();
        var nextHeight = currentState.CurrentHeight;
        var startCount = 0;
        while (startCount++ < _blockCount && !CancellationToken.IsCancellationRequested &&
               currentState.State == SyncState.AsyncRunning)
        {
            if (await blockMessageService.SendMessageAsync(nextHeight, CancellationToken))
            {
                nextHeight++;
            }
            else
            {
                await _syncBlockStateProvider.UpdateStateAsync(null, SyncState.SyncPrepared,
                    SyncState.AsyncRunning);
            }

            currentState = await _syncBlockStateProvider.GetCurrentStateAsync();
        }
    }
}