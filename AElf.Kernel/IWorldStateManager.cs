using System.Collections.Generic;
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
        Task<IWorldState> GetWorldStateAsync(Hash chainId, Hash blockHash);

        /// <summary>
        /// Set the world state.
        /// The currentBlockHash is next _preBlockHash.
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="currentBlockHash"></param>
        /// <returns></returns>
        Task SetWorldStateToCurrentState(Hash chainId, Hash currentBlockHash);

        Task UpdatePointer(Hash pathHash, Hash pointerHash);
        
        Task<Hash> GetPointer(Hash pathHash);
        Hash GetPointer(Path path);
        
        Task InsertChange(Hash pathHash, Hash hashBefore, Hash pointerHash);
        
        /// <summary>
        /// Rollback to previous world state.
        /// </summary>
        /// <returns></returns>
        Task RollbackDataToPreviousWorldState();

        Task<List<Hash>> GetFixedPathsAsync(Hash blockHash = null);

        Task<List<Change>> GetChangesAsync(Hash chainId, Hash blockHash);

        Task<List<Change>> GetChangesAsync();

        Task<Dictionary<Hash, Change>> GetChangesDictionaryAsync();
        
        IAccountDataProvider GetAccountDataProvider(Hash chain, Hash account);

        Task SetData(Hash pointerHash, byte[] data);

        Task<byte[]> GetData(Hash pointerHash);
    }
}