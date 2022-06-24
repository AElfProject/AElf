using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AElf.WebApp.MessageQueue.Entities;
using AElf.WebApp.MessageQueue.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Volo.Abp.DependencyInjection;

namespace AElf.WebApp.MessageQueue
{
public interface IEventSendTaskManager
{
    bool Start(IEnumerable<EventFilterEntity> eventFilters);
    Task<bool> StopAsync();
    bool IsStopped();
}

public class EventSendTaskManager : IEventSendTaskManager, ISingletonDependency
{
    private readonly ConcurrentBag<Task> _taskList;
    private readonly int _maxWorkerCount;
    private readonly ConcurrentQueue<List<EventFilterEntity>> _eventFiltersQueue;
    private CancellationTokenSource _stopCancellationTokenSource;
    private readonly object _lock;
    private Task _startTask;
    private readonly ILogger<EventSendTaskManager> _logger;
    private IServiceScopeFactory ServiceScopeFactory { get; }

    public EventSendTaskManager(IServiceScopeFactory serviceScopeFactory, ILogger<EventSendTaskManager> logger)
    {
        ServiceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _taskList = new ConcurrentBag<Task>();
        _maxWorkerCount = 10;
        _eventFiltersQueue = new ConcurrentQueue<List<EventFilterEntity>>();
        TaskFactory = AddTask;
        _lock = new object();
    }

    public bool Start(IEnumerable<EventFilterEntity> eventFilterEnumerable)
    {
        if (eventFilterEnumerable == null)
        {
            return false;
        }
        var eventFilter = eventFilterEnumerable.ToList();
        if (!eventFilter.Any())
        {
            return false;
        }
        
        if (_stopCancellationTokenSource != null)
        {
            return false;
        }

        CancellationToken ctsToken;
        lock (_lock)
        {
            if (_stopCancellationTokenSource != null)
            {
                return false;
            }

            _stopCancellationTokenSource = new CancellationTokenSource();
            ctsToken = _stopCancellationTokenSource.Token;
        }

        if (ctsToken == CancellationToken.None)
        {
            return false;
        }

        _logger.LogInformation("start dispatching event filter task");
        _startTask = StartAsync(eventFilter, ctsToken);
        return true;
    }

    private async Task StartAsync(IEnumerable<EventFilterEntity> eventFilters, CancellationToken ctsToken)
    {
        InitializeEventFilters(eventFilters);
        var totalTaskCount = _eventFiltersQueue.Count;
        if (totalTaskCount <= _maxWorkerCount)
        {
            while (TryToAttachTask(false, ctsToken))
            {
            }

            await _taskList.WhenAll();
            return;
        }

        while (!ctsToken.IsCancellationRequested)
        {
            if (_taskList.Count >= _maxWorkerCount)
            {
                await _taskList.WhenAny();
                TryToAttachTask(true, ctsToken);
            }

            if (!TryToAttachTask(true, ctsToken))
            {
                await _taskList.WhenAny();
            }
        }
        
        await _taskList.WhenAll();
        
        bool TryToAttachTask(bool isTempTask, CancellationToken ct)
        {
            if (!_eventFiltersQueue.TryDequeue(out var efs)) return false;
            if (isTempTask)
            {
                _taskList.Add(this.TaskFactory(efs, true, ct));
                return true;
            }

            _taskList.Add(this.TaskFactory(efs, false, ct));
            return true;
        }
    }

    public async Task<bool> StopAsync()
    {
        if (_stopCancellationTokenSource == null)
        {
            _logger.LogInformation("Stopped");
            return false;
        }

        _stopCancellationTokenSource.Cancel();
        _logger.LogInformation("Stopping");
        await _startTask;
        _taskList.Clear();
        _eventFiltersQueue.Clear();
        _stopCancellationTokenSource = null;
        _logger.LogInformation("Stopped");
        return true;
    }

    public bool IsStopped()
    {
        return _stopCancellationTokenSource == null ||
               (_stopCancellationTokenSource.IsCancellationRequested && _taskList.IsEmpty && _eventFiltersQueue.IsEmpty);
    }

    private void InitializeEventFilters(IEnumerable<EventFilterEntity> eventFilter)
    {
        _eventFiltersQueue.Clear();

        foreach (var groupedEventFilters in eventFilter.GroupBy(x => x.CurrentHeight).OrderBy(p => p.Key))
        {
            _eventFiltersQueue.Enqueue(groupedEventFilters.ToList());
        }
    }

    private async Task ProcessLimitEventFiltersAsync(List<EventFilterEntity> eventFilters, CancellationToken ctsToken)
    {
        await HandleEventFilters(eventFilters, ctsToken);
        var logMessage = new StringBuilder();
        logMessage.Append("requeue event filer:\n");
        
        foreach (var eventFilter in eventFilters)
        {
            logMessage.Append($"event filer id: {eventFilter.Id}\n");
        }
        _logger.LogInformation(logMessage.ToString());
        _eventFiltersQueue.Enqueue(eventFilters);
    }

    private async Task ProcessEventFiltersAsync(List<EventFilterEntity> eventFilters, CancellationToken ctsToken)
    {
        while (!ctsToken.IsCancellationRequested)
        {
            await HandleEventFilters(eventFilters, ctsToken);
        }
    }

    private async Task HandleEventFilters(List<EventFilterEntity> eventFilter, CancellationToken ctsToken)
    {
        using var scope = ServiceScopeFactory.CreateScope();
        var syncEventFilterProcess = scope.ServiceProvider.GetRequiredService<ISendEventAsyncService>();
        await syncEventFilterProcess.ProcessEventFilters(eventFilter, ctsToken);
    }

    private Task AddTask(List<EventFilterEntity> eventFilters, bool isTemp, CancellationToken ctsToken)
    {
        return isTemp
            ? ProcessLimitEventFiltersAsync(eventFilters, ctsToken)
            : ProcessEventFiltersAsync(eventFilters, ctsToken);
    }

    public Func<List<EventFilterEntity>, bool, CancellationToken, Task> TaskFactory { get; set; }
}
}