using System.Threading.Tasks;
using AElf.WebApp.MessageQueue.Enum;

namespace AElf.WebApp.MessageQueue.Provider;

public interface ISyncBlockStateProvider
{
    bool IsStopped();
    Task<long> GetCurrentHeightAsync();
    Task UpdateAsync(long height);

    Task SetStateAsync(long height);
    Task SetStateAsync(SyncState state);
}

public class SyncBlockStateProvider : ISyncBlockStateProvider
{
    public bool IsStopped()
    {
        throw new System.NotImplementedException();
    }

    public Task<long> GetCurrentHeightAsync()
    {
        throw new System.NotImplementedException();
    }

    public Task UpdateAsync(long height)
    {
        throw new System.NotImplementedException();
    }

    public Task SetStateAsync(long height)
    {
        throw new System.NotImplementedException();
    }

    public Task SetStateAsync(SyncState state)
    {
        throw new System.NotImplementedException();
    }
}

public class SyncInformation
{
    public long CurrentHeight { get; set; }
    public SyncState State { get; set; }
}