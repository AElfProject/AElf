using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISyncCacheService
    {
        Task SyncCache(IChainContext chainContext);
    }

    public class SyncCacheService : ISyncCacheService
    {
        private readonly IServiceContainer<ISyncCacheProvider> _syncCacheProviders;
        public ILogger<SyncCacheService> Logger { get; set; }

        public SyncCacheService(IServiceContainer<ISyncCacheProvider> syncCacheProviders)
        {
            _syncCacheProviders = syncCacheProviders;
            Logger = NullLogger<SyncCacheService>.Instance;
        }

        public async Task SyncCache(IChainContext chainContext)
        {
            foreach (var syncCacheProvider in _syncCacheProviders)
            {
                Logger.LogInformation($"Syncing cache via {syncCacheProvider.GetType().FullName}");
                await syncCacheProvider.SyncCache(chainContext);
            }
        }
    }
}