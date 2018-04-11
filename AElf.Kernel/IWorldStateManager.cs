using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IWorldStateManager
    {
        Hash GenesisBlockHash { get; set; }

        /// <summary>
        /// Get the world state of a chain
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        Task<WorldState> GetWorldStateAsync(Hash chainId);

        IAccountDataProvider GetAccountDataProvider(Hash chain, Hash account);
    }
}