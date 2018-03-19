using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IWorldStateStore
    {
        /// <summary>
        /// Store current world state.
        /// </summary>
        /// <returns></returns>
        Task SetWorldStateAsync();
        
        /// <summary>
        /// Get the world state by corresponding block height.
        /// </summary>
        /// <param name="height"></param>
        /// <returns></returns>
        Task<IWorldState> GetAsync(long height);

        /// <summary>
        /// Get latest world state.
        /// </summary>
        /// <returns></returns>
        Task<IWorldState> GetAsync();
    }
}