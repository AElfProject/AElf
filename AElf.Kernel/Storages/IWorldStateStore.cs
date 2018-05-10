using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IWorldStateStore
    {
        Task InsertWorldStateAsync(Hash chainId, Hash blockHash, ChangesDict changesStore);
        Task<WorldState> GetWorldStateAsync(Hash chainId, Hash blockHash);
    }
}