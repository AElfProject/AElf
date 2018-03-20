using System;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public class WorldStateManager: IWorldStateManager
    {
        private readonly Func<IAccountDataProvider> _factory;

        public WorldStateManager(Func<IAccountDataProvider> factory,IAccountDataProvider p)
        {
            _factory = factory;

            var a = factory();
            var b = factory();
            if (a != b)
            {
                //
            }
            
            
        }


        public Task<IWorldState> GetWorldStateAsync(IHash<IChain> chain)
        {
            throw new NotImplementedException();
        }

        public IAccountDataProvider GetAccountDataProvider(IHash<IChain> chain, IHash<IAccount> account)
        {
            throw new NotImplementedException();
        }
    }
    
    
}