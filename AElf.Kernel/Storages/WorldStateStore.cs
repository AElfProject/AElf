using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public class WorldStateStore : IWorldStateStore
    {
        private readonly Dictionary<IHash, List<IChangesStore>> _changesStores = 
            new Dictionary<IHash, List<IChangesStore>>();
        
        private Dictionary<IHash, List<IAccountDataProvider>> _accountDataProviders =
            new Dictionary<IHash, List<IAccountDataProvider>>();
        
        public Task SetWorldStateAsync(IHash chainHash, IChangesStore changesStore)
        {
            if (!Validation(changesStore))
            {
                throw new InvalidOperationException("Invalide changes");
            }
            
            _changesStores[chainHash].Add(changesStore);
            return Task.CompletedTask;
        }

        public Task<WorldState> GetAsync(IHash chainHash, long height)
        {
            throw new System.NotImplementedException();
        }

        public Task<WorldState> GetAsync(IHash chainHash)
        {
            return Task.FromResult(CreateWorldState(chainHash));
        }

        private bool Validation(IChangesStore changesStore)
        {
            return true;
        }

        private List<IChangesStore> GetChangesList(IHash chainHash)
        {
            return _changesStores[chainHash];
        }

        private WorldState CreateWorldState(IHash chainHash, long height = -1)
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