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

        /// <inheritdoc />
        public Task<IWorldState> GetWorldStateAsync(IChain chain)
        {
            throw new NotImplementedException();
        }

        public IAccountDataProvider GetAccountDataProvider(IChain chain, IAccount account)
        {
            var p = _factory();
            
            p.Context = new AccountDataContext();
            
            return p;
        }
    }
}