using System.Threading.Tasks;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISyncCacheProvider
    {
        Task SyncCacheAsync(IChainContext chainContext);
    }
}