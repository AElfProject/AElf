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
    private readonly ISyncBlockLatestHeightProvider _latestHeightProvider;
    protected CancellationToken CancellationToken { get; set; }
    private int _blockCount;

    public SendMessageWorker(ISyncBlockStateProvider syncBlockStateProvider, AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory, IOptionsSnapshot<MessageQueueOptions> option,
        ISyncBlockLatestHeightProvider latestHeightProvider) : base(timer,
        serviceScopeFactory)
    {
        _syncBlockStateProvider = syncBlockStateProvider;
        _latestHeightProvider = latestHeightProvider;
        _blockCount = option.Value.BlockCountPerPeriod;
        Timer.Period = option.Value.Period;
        timer.RunOnStart = true;
    }

    public void SetWork(int? period, int? blockCountPerPeriod)
    {
        if (period.HasValue)
        {
            Timer.Period = period.Value;
        }

        if (blockCountPerPeriod.HasValue)
        {
            _blockCount = blockCountPerPeriod.Value;
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await base.StartAsync(cancellationToken);
        CancellationToken = cancellationToken;
    }

    public Task StopTimerAsync(CancellationToken cancellationToken = default)
    {
        Timer.Stop(cancellationToken);
        return Task.CompletedTask;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var blockMessageService = workerContext.ServiceProvider.GetRequiredService<IBlockMessageService>();
        var currentState = await _syncBlockStateProvider.GetCurrentStateAsync();
        var nextHeight = currentState.CurrentHeight;
        var startCount = 0;
        while (IsContinue(startCount++, currentState.State))
        {
            var latestHeight = _latestHeightProvider.GetLatestHeight();
            if (nextHeight > latestHeight - 4)
            {
                await PreparedToSyncMessageAsync();
                break;
            }
            
            if (await blockMessageService.SendMessageAsync(nextHeight, CancellationToken))
            {
                nextHeight++;
            }
            else
            {
                await PreparedToSyncMessageAsync();
                break;
            }

            currentState = await _syncBlockStateProvider.GetCurrentStateAsync();
        }
    }

    private bool IsContinue(long height, SyncState state)
    {
        return height < _blockCount && !CancellationToken.IsCancellationRequested &&
               state == SyncState.AsyncRunning;
    }

    private async Task PreparedToSyncMessageAsync()
    {
        await _syncBlockStateProvider.UpdateStateAsync(null, SyncState.SyncPrepared,
            SyncState.AsyncRunning);
    }
}