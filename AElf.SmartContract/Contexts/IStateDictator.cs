using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using Google.Protobuf;

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
  IAccountDataProvider GetAccountDataProvider(Hash accountAddress);

  /*
   * WorldState
   */
  Task<WorldState> GetLatestWorldStateAsync();
  Task<WorldState> GetWorldStateAsync(Hash stateHash);
  Task SetWorldStateAsync();

  /*
   * Hash
   */
  Task<Hash> GetHashAsync(Hash hash);
  Task SetHashAsync(Hash origin, Hash another);

  /*
   * Data operation
   */
  Task SetDataAsync<T>(Hash pointerHash, T data) where T : IMessage;
  Task<T> GetDataAsync<T>(Hash pointerHash) where T : IMessage, new();

  /*
   * Chain height
   */
  Task<ulong> GetChainCurrentHeightAsync(Hash chainId);
  Task SetChainCurrentHeightAsync(Hash chainId, ulong height);

  /*
   * Block hash - State hash
   */
  Task<Hash> GetBlockHashAsync(Hash stateHash);
  Task SetBlockHashAsync(Hash stateHash, Hash blockHash);
  Task<Hash> GetStateHashAsync(Hash blockHash);
  Task SetStateHashAsync(Hash blockHash, Hash stateHash);

  Task RollbackToPreviousBlock();
  Task ApplyStateValueChangeAsync(StateValueChange stateValueChange);
  Task<bool> ApplyCachedDataAction(Dictionary<DataPath, StateCache> queue);
  Hash ChainId { get; set; }
  ulong CurrentRoundNumber { get; set; }
  Hash BlockProducerAccountAddress { get; set; }
 }
}