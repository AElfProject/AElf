using System.Threading.Tasks;

namespace AElf.Kernel.Managers
{
    public interface IWorldStateManager
    {
        Task<IWorldState> GetWorldStateAsync(Hash stateHash);
        Task SetWorldStateAsync(Hash stateHash, WorldState worldState);
    }
}