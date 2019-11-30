using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IForkCacheService
    {
        Task MergeAndCleanForkCacheAsync(Hash irreversibleBlockHash, long irreversibleBlockHeight);
    }
}