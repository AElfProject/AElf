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

    public class WorldStateStore : IWorldStateStore
    {
        public Task SetWorldStateAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task<IWorldState> GetAsync(long height)
        {
            throw new System.NotImplementedException();
        }

        public Task<IWorldState> GetAsync()
        {
            throw new System.NotImplementedException();
        }
    }
}