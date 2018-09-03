using System.Threading.Tasks;
using AElf.Kernel.Storages;

namespace AElf.Kernel.Managers
{
    public class WorldStateManager : IWorldStateManager
    {
        private readonly IDataStore _dataStore;

        public WorldStateManager(IDataStore dataStore)
        {
            _dataStore = dataStore;
        }

        public async Task<IWorldState> GetWorldStateAsync(Hash stateHash)
        {
            return await _dataStore.GetAsync<WorldState>(stateHash);
        }

        public async Task SetWorldStateAsync(Hash stateHash, WorldState worldState)
        {
            await _dataStore.InsertAsync(stateHash, worldState);
        }
    }
}