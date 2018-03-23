using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IWorldStateStore
    {
        /// <summary>
        /// Store current world state.
        /// </summary>
        /// <returns></returns>
        Task SetWorldStateAsync(IHash<IChain> chainHash, IChangesStore changesStore);

        /// <summary>
        /// Get the world state by corresponding block height of corresponding chain.
        /// </summary>
        /// <param name="chainHash"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        Task<WorldState> GetAsync(IHash<IChain> chainHash, long height);

        /// <summary>
        /// Get latest world state of corresponding chain.
        /// </summary>
        /// <returns></returns>
        Task<WorldState> GetAsync(IHash<IChain> chainHash);
    }

    public class WorldStateStore : IWorldStateStore
    {
        private readonly Dictionary<IHash<IChain>, List<IChangesStore>> _changesStores = 
            new Dictionary<IHash<IChain>, List<IChangesStore>>();
        
        private Dictionary<IHash<IChain>, List<IAccountDataProvider>> _accountDataProviders =
            new Dictionary<IHash<IChain>, List<IAccountDataProvider>>();
        
        public Task SetWorldStateAsync(IHash<IChain> chainHash, IChangesStore changesStore)
        {
            if (!Validation(changesStore))
            {
                throw new InvalidOperationException("Invalide changes");
            }
            
            _changesStores[chainHash].Add(changesStore);
            return Task.CompletedTask;
        }

        public Task<WorldState> GetAsync(IHash<IChain> chainHash, long height)
        {
            throw new System.NotImplementedException();
        }

        public Task<WorldState> GetAsync(IHash<IChain> chainHash)
        {
            return Task.FromResult(CreateWorldState(chainHash));
        }

        private bool Validation(IChangesStore changesStore)
        {
            return true;
        }

        private List<IChangesStore> GetChangesList(IHash<IChain> chainHash)
        {
            return _changesStores[chainHash];
        }

        private WorldState CreateWorldState(IHash<IChain> chainHash, long height = -1)
        {
            var currentWorldState = new WorldState(_accountDataProviders[chainHash]);
            if (height == -1)
            {
                return currentWorldState;
            }

            var changes = GetChangesList(chainHash);
            var currentHeight = changes.Count;
            var changesToRollback = changes.GetRange((int)height, (int)(currentHeight - height));
            return Rollback(currentWorldState, changesToRollback);
        }

        private WorldState Rollback(WorldState worldState, List<IChangesStore> changes)
        {
            throw new NotImplementedException();
        }
    }
}