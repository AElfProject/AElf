using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Managers
{
    public interface IWorldStateManager
    {
        Task<IWorldState> GetWorldStateAsync(Hash chainId, Hash blockHash);

        Task SetWorldStateToCurrentStateAsync(Hash chainId, Hash currentBlockHash);

        Task UpdatePointerToPointerStoreAsync(Hash pathHash, Hash pointerHash);
        
        Task<Hash> GetPointerFromPointerStoreAsync(Hash pathHash);
        
        Hash CalculatePointerHashOfCurrentHeight(Path path);
        
        Task InsertChangeAsync(Hash pathHash, Change change);
        
        Task RollbackDataToPreviousWorldStateAsync();

        Task<List<Hash>> GetPathsAsync(Hash blockHash = null);

        Task<List<Change>> GetChangesAsync(Hash chainId, Hash blockHash);

        Task<List<Change>> GetChangesAsync();

        Task<Dictionary<Hash, Change>> GetChangesDictionaryAsync();

        IAccountDataProvider GetAccountDataProvider(Hash chain, Hash account);

        Task SetDataAsync(Hash pointerHash, byte[] data);

        Task<byte[]> GetDataAsync(Hash pointerHash);
    }
}