using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IWorldStateManager
    {
        Task<IWorldState> GetWorldStateAsync(Hash chainId, Hash blockHash);

        Task SetWorldStateToCurrentState(Hash chainId, Hash currentBlockHash);

        Task UpdatePointerToPointerStore(Hash pathHash, Hash pointerHash);
        
        Task<Hash> GetPointerFromPointerStore(Hash pathHash);
        
        Hash CalculatePointerHashOfCurrentHeight(Path path);
        
        Task InsertChange(Hash pathHash, Change change);
        
        Task RollbackDataToPreviousWorldState();

        Task<List<Hash>> GetPathsAsync(Hash blockHash = null);

        Task<List<Change>> GetChangesAsync(Hash chainId, Hash blockHash);

        Task<List<Change>> GetChangesAsync();

        Task<Dictionary<Hash, Change>> GetChangesDictionaryAsync();

        IAccountDataProvider GetAccountDataProvider(Hash chain, Hash account);

        Task SetData(Hash pointerHash, byte[] data);

        Task<byte[]> GetData(Hash pointerHash);
    }
}