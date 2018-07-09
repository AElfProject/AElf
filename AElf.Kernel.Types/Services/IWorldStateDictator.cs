using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Types;

namespace AElf.Kernel.Managers
{
    public interface IWorldStateDictator
    {
        bool DeleteChangeBeforesImmidiately { get; set; }
        
        IWorldStateDictator SetChainId(Hash chainId);
        
        Task<IWorldState> GetWorldStateAsync(Hash blockHash);

        Task SetWorldStateAsync(Hash currentBlockHash);

        Task UpdatePointerAsync(Hash pathHash, Hash pointerHash);
        
        Task<Hash> GetPointerAsync(Hash pathHash);
        
        Task<Hash> CalculatePointerHashOfCurrentHeight(PathContext pathContext);
        
        Task InsertChangeAsync(Hash pathHash, Change change);

        Task<Change> GetChangeAsync(Hash pathHash);
        
        Task RollbackCurrentChangesAsync();

        Task RollbackToSpecificHeight(ulong specificHeight);

        Task<List<Hash>> GetPathsAsync(Hash blockHash = null);

        Task<List<Change>> GetChangesAsync(Hash blockHash);

        Task<List<Change>> GetChangesAsync();

        Task<Dictionary<Hash, Change>> GetChangesDictionaryAsync();

        Task<IAccountDataProvider> GetAccountDataProvider(Hash accountAddress);

        Task SetDataAsync(Hash pointerHash, byte[] data);

        Task<byte[]> GetDataAsync(Hash pointerHash);

        Task<Change> ApplyStateValueChangeAsync(StateValueChange stateValueChange, Hash chainId);
        
        Hash PreBlockHash { get; set; }
    }
}