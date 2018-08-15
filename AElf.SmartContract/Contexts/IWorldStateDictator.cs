using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.SmartContract
{
    public interface IWorldStateDictator
    {
        IWorldStateDictator SetChainId(Hash chainId);
        
        Task<IWorldState> GetWorldStateAsync(Hash blockHash);

        Task SetWorldStateAsync(Hash currentBlockHash);

        Task UpdatePointerAsync(Hash pathHash, Hash pointerHash);
        
        Task<Hash> GetPointerAsync(Hash pathHash);
        
        Task<Hash> CalculatePointerHashOfCurrentHeight(IResourcePath resourcePath);
        
        Task RollbackToPreviousBlock();

        Task RollbackToBlockHash(Hash blockHash);
        
        Task<List<Transaction>> RollbackToSpecificHeight(ulong specificHeight);

        Task<IAccountDataProvider> GetAccountDataProvider(Hash accountAddress);

        Task SetDataAsync(Hash pointerHash, byte[] data);

        Task<byte[]> GetDataAsync(Hash pointerHash);

        Task ApplyStateValueChangeAsync(StateValueChange stateValueChange, Hash chainId);

        Task<bool> ApplyCachedDataAction(Dictionary<Hash, StateCache> queue, Hash chainId);
        
        Hash PreBlockHash { get; set; }
        
        Hash BlockProducerAccountAddress { get; set; }

    }
}