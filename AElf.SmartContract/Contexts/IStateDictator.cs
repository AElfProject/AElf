using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using Google.Protobuf;
using AElf.Common;

// ReSharper disable once CheckNamespace
namespace AElf.SmartContract
{
    /// <summary>
    /// Act as a service for DataProviders:
    /// Get / Set WorldState
    /// Get / Set Hash
    /// Get / Set Data
    /// Get AccountDataProvider
    /// Formulate StateHash in ResourcePath instance
    /// Rollback
    /// </summary>
    public interface IStateDictator
    {
        IAccountDataProvider GetAccountDataProvider(Address accountAddress);

        Task<WorldState> GetLatestWorldStateAsync();
        Task<WorldState> GetWorldStateAsync(Hash stateHash);
        Task SetWorldStateAsync();

        Task<Hash> GetHashAsync(Hash hash);
        Task SetHashAsync(Hash origin, Hash another);

        Task SetDataAsync<T>(Hash pointerHash, T data) where T : IMessage;
        Task<T> GetDataAsync<T>(Hash pointerHash) where T : IMessage, new();

        Task SetMap(Hash blockHash);
        Task<Hash> GetBlockHashAsync(Hash stateHash);
        Task<Hash> GetStateHashAsync(Hash blockHash);

        Task RollbackToPreviousBlock();
        Task ApplyStateValueChangeAsync(StateValueChange stateValueChange);
        Task<bool> ApplyCachedDataAction(Dictionary<DataPath, StateCache> queue);
        Hash ChainId { get; set; }
        ulong BlockHeight { get; set; }
        Address BlockProducerAccountAddress { get; set; }
    }
}