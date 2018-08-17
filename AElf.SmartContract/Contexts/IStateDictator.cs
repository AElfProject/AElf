using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;

// ReSharper disable once CheckNamespace
namespace AElf.SmartContract
{
    /// <summary>
    /// Act as a service for DataProviders:
    /// Get / Set WorldState
    /// Formulate StateHash in ResourcePath instance
    /// </summary>
    public interface IStateDictator
    {
        Task<WorldState> GetWorldStateAsync(Hash stateHash);

        Task SetWorldStateAsync(Hash stateHash, WorldState worldState);
        
        Task<Hash> GetPointerAsync(Hash pathHash);
        
        Task RollbackToPreviousBlock();

        Task<IAccountDataProvider> GetAccountDataProvider(Hash accountAddress);

        Task SetDataAsync(Hash pointerHash, byte[] data);

        Task<byte[]> GetDataAsync(Hash pointerHash);

        Task ApplyStateValueChangeAsync(StateValueChange stateValueChange, Hash chainId);

        Task<bool> ApplyCachedDataAction(Dictionary<Hash, StateCache> queue, Hash chainId);
        
        ulong CurrentRoundNumber { get; set; }
        
        Hash BlockProducerAccountAddress { get; set; }
    }
}