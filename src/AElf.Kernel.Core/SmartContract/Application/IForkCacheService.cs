using System.Threading.Tasks;
using AElf.Types;

//TODO: namespace seems not correct
namespace AElf.Kernel.SmartContract.Application
{
    //TODO: why do we need?
    public interface IForkCacheService
    {
        Task MergeAndCleanForkCacheAsync(Hash irreversibleBlockHash, long irreversibleBlockHeight);
    }
}