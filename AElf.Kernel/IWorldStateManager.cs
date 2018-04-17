using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IWorldStateManager
    {
        /// <summary>
        /// Get the current world state of a chain.
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        Task<WorldState> GetWorldStateAsync(Hash chainId);

        /// <summary>
        /// Get the world state of specific block.
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="blockHash"></param>
        /// <returns></returns>
        Task<WorldState> GetWorldStateAsync(Hash chainId, Hash blockHash);

        IAccountDataProvider GetAccountDataProvider(Hash chain, Hash account);

        Task SetData(Hash pointerHash, byte[] data);

        Task<byte[]> GetData(Hash pointerHash);
    }
}