using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IWorldStateManager
    {
        /// <summary>
        /// Get the world state of specific previous block.
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="blockHash"></param>
        /// <returns></returns>
        Task<WorldState> GetWorldStateAsync(Hash chainId, Hash blockHash);

        /// <summary>
        /// Set the world state.
        /// The currentBlockHash is next _preBlockHash.
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="currentBlockHash"></param>
        /// <returns></returns>
        Task SetWorldStateToCurrentState(Hash chainId, Hash currentBlockHash);

        /// <summary>
        /// Rollback to previous world state.
        /// </summary>
        /// <returns></returns>
        Task RollbackDataToPreviousWorldState();
        
        IAccountDataProvider GetAccountDataProvider(Hash chain, Hash account);

        Task SetData(Hash pointerHash, byte[] data);

        Task<byte[]> GetData(Hash pointerHash);
    }
}