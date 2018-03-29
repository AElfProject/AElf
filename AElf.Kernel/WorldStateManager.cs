using System;
using System.Threading.Tasks;
using AElf.Kernel.Storages;

namespace AElf.Kernel
{
    /// <summary>
    /// TODO:
    /// Cache
    /// </summary>
    public class WorldStateManager: IWorldStateManager
    {
        private readonly Func<IAccountDataProvider> _factory;
        private readonly IWorldStateStore _worldStateStoreStore;

        public WorldStateManager(Func<IAccountDataProvider> factory)
        {
            _factory = factory;
        }

        public Task<IWorldState> GetWorldStateAsync(IHash chain)
        {
            throw new NotImplementedException();
        }

        public IAccountDataProvider GetAccountDataProvider(IHash chain, IHash account)
        {
            throw new NotImplementedException();
        }
    }
}