using System.Threading;
using Volo.Abp.DependencyInjection;

namespace AElf.WebApp.MessageQueue.Provider;

public interface ISyncBlockLatestHeightProvider
{
    void SetLatestHeight(long height);
    long GetLatestHeight();
}

public class SyncBlockLatestHeightProvider : ISyncBlockLatestHeightProvider, ISingletonDependency
{
    private long _latestHeight;
    private int _lockFlag;

    public void SetLatestHeight(long height)
    {
        if (height < 1)
        {
            return;
        }

        while (IsEnter())
        {
        }

        _latestHeight = height;
        Reset();
    }

    public long GetLatestHeight()
    {
        while (IsEnter())
        {
        }

        var latestHeight = _latestHeight;
        Reset();
        return latestHeight;
    }

    private bool IsEnter()
    {
        return Interlocked.CompareExchange(ref _lockFlag, 1, 0) == 1;
    }

    private void Reset()
    {
        _lockFlag = 0;
    }
}