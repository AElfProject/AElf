using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IWorldStateManager
    {
        /// <summary>
        /// Get current world state of a chain
        /// </summary>
        /// <param name="chain"></param>
        /// <returns></returns>
        Task<IWorldState> GetWorldStateAsync(IChain chain);

        /// <summary>
        /// Get current AccountDataProvider of an account in a chain.
        /// </summary>
        /// <param name="chain"></param>
        /// <param name="account"></param>
        /// <returns></returns>
        IAccountDataProvider GetAccountDataProvider(IChain chain, IAccount account);
    }
}