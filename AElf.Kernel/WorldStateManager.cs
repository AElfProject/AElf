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