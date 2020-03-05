using System.Threading.Tasks;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISyncCacheProvider
    {
        Task SyncCache(IChainContext chainContext);
    }
}