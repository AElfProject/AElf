using System.Threading.Tasks;

namespace AElf.Kernel
{
    public class WorldStateManager: IWorldStateManager
    {
        public Task<IWorldState> GetWorldStateAsync(IChain chain)
        {
            throw new System.NotImplementedException();
        }

        public IAccountDataProvider GetAccountDataProvider(IChain chain, IAccount account)
        {
            throw new System.NotImplementedException();
        }
    }
    
    
}