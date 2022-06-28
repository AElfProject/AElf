using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using AElf.WebApp.MessageQueue.Enum;
using AElf.WebApp.MessageQueue.Provider;
using AElf.WebApp.MessageQueue.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ExceptionHandling;

namespace AElf.WebApp.MessageQueue;

public class SendMessageWorker : ISingletonDependency
{
    public IAbpLazyServiceProvider LazyServiceProvider { get; set; }
    protected IServiceScopeFactory ServiceScopeFactory { get; }
    protected ILoggerFactory LoggerFactory => LazyServiceProvider.LazyGetRequiredService<ILoggerFactory>();

    protected ILogger Logger => LazyServiceProvider.LazyGetService<ILogger>(provider =>
        LoggerFactory?.CreateLogger(GetType().FullName) ?? NullLogger.Instance);

    private readonly ISyncBlockStateProvider _syncBlockStateProvider;

    public SendMessageWorker(IServiceScopeFactory serviceScopeFactory, ISyncBlockStateProvider syncBlockStateProvider)
    {
        ServiceScopeFactory = serviceScopeFactory;
        _syncBlockStateProvider = syncBlockStateProvider;
    }

    public async Task StartAsync(long height, CancellationToken cancellationToken)
    {
        await Task.Yield();
        var nextHeight = height;

        using var scope = ServiceScopeFactory.CreateScope();
        try
        {
            var blockMessageService = scope.ServiceProvider.GetRequiredService<IBlockMessageService>();
            var currentState = _syncBlockStateProvider.GetCurrentState();
            while (!cancellationToken.IsCancellationRequested && currentState.State == SyncState.Running)
            {
                if (await blockMessageService.SendMessageAsync(nextHeight, cancellationToken))
                {
                    nextHeight++;
                }
                else
                {
                    await _syncBlockStateProvider.UpdateStateAsync(null, SyncState.Prepared);
                }

                currentState = _syncBlockStateProvider.GetCurrentState();
            }
        }
        catch (Exception ex)
        {
            await scope.ServiceProvider
                .GetRequiredService<IExceptionNotifier>()
                .NotifyAsync(new ExceptionNotificationContext(ex));

            Logger.LogException(ex);
        }
    }
}