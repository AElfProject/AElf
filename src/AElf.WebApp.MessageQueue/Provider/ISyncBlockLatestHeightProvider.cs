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

    public void SetLatestHeight(long height)
    {
        if (height < 1)
        {
            return;
        }

        Interlocked.Exchange(ref _latestHeight, height);
    }

    public long GetLatestHeight()
    {
        return Interlocked.Read(ref _latestHeight);
    }
}