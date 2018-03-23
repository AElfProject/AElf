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
        Task<IWorldState> GetWorldStateAsync(IHash chain);

        IAccountDataProvider GetAccountDataProvider(IHash chain, IHash account);
    }
}