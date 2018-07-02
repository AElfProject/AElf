using System.Threading.Tasks;
using AElf.Kernel.Types;

namespace AElf.Kernel.Storages
{
    public interface IWorldStateStore
    {
        Task InsertWorldStateAsync(Hash chainId, Hash blockHash, ChangesDict changesStore);
        Task<WorldState> GetWorldStateAsync(Hash chainId, Hash blockHash);
    }
}