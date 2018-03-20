using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IWorldStateManager
    {
        /// <summary>
        /// Get the world state of a chain
        /// </summary>
        /// <param name="chain"></param>
        /// <returns></returns>
        Task<IWorldState> GetWorldStateAsync(IHash<IChain> chain);

        IAccountDataProvider GetAccountDataProvider(IHash<IChain> chain, IHash<IAccount> account);
    }
}