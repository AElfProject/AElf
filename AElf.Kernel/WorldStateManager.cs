using System;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public class WorldStateManager: IWorldStateManager
    {
        private readonly Func<IAccountDataProvider> _factory;

        public WorldStateManager(Func<IAccountDataProvider> factory)
        {
            _factory = factory;
        }

        public Task<IWorldState> GetWorldStateAsync(IChain chain)
        {
            throw new System.NotImplementedException();
        }

        public IAccountDataProvider GetAccountDataProvider(IChain chain, IAccount account)
        {
            var p = _factory();
            
            throw new System.NotImplementedException();
        }
    }
    
    
}