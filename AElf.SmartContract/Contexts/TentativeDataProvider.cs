using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf;
using AElf.Kernel;

namespace AElf.SmartContract
{
    public class TentativeDataProvider : ITentativeDataProvider
    {
        private IDataProvider _dataProvider;

        private List<TentativeDataProvider> _children = new List<TentativeDataProvider>();

        private readonly Dictionary<Hash, StateCache> _tentativeCache = new Dictionary<Hash, StateCache>();
        
        //Injected from outside for the entry data provider of the executive ( in worker actor )
        public Dictionary<Hash, StateCache> StateCache
        {
            get => _stateCache;
            set
            {
                _stateCache = value;
                foreach (var dataProvider in _children)
                {
                    dataProvider.StateCache = value;
                }
            }
        }

        private Dictionary<Hash, StateCache> _stateCache;


        private async Task<StateCache> GetStateAsync(Hash keyHash)
        {
            if (!_tentativeCache.TryGetValue(keyHash, out var state))
            {
                if (!StateCache.TryGetValue(GetPathFor(keyHash), out state))
                {
                    state = new StateCache(await _dataProvider.GetAsync(keyHash));
                    StateCache.Add(GetPathFor(keyHash), state);
                }
                _tentativeCache.Add(keyHash, state);
            }

            return state;
        }

        public TentativeDataProvider(IDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
            
            //initialize the state cache to empty in case we choose not to use tx-shared cache
            StateCache = new Dictionary<Hash, StateCache>(); 
        }

        public IDataProvider GetDataProvider(string name)
        {
            var dp = new TentativeDataProvider(_dataProvider.GetDataProvider(name));
            dp.StateCache = StateCache;
            _children.Add(dp);
            return dp;
        }

        public async Task SetAsync(Hash keyHash, byte[] obj)
        {
            var state = await GetStateAsync(keyHash);
            state.CurrentValue = obj;
        }

        public async Task<byte[]> GetAsync(Hash keyHash)
        {
            return (await GetStateAsync(keyHash)).CurrentValue ?? new byte[0];
        }

        public async Task<byte[]> GetAsync(Hash keyHash, Hash preBlockHash)
        {
            // This method seems not necessary here.
            // It is not required for contract execution.
            // It may only be required if user wants to retrieve
            // previous state via rpc.
            throw new NotImplementedException();
            await Task.CompletedTask;
        }

        public Hash GetHash()
        {
            // This method is not needed here.
            throw new NotImplementedException();
        }

        public Hash GetPathFor(Hash keyHash)
        {
            return _dataProvider.GetPathFor(keyHash);
        }

        public IEnumerable<StateValueChange> GetValueChanges()
        {
            var changes = new List<StateValueChange>();
            foreach (var keyState in _tentativeCache)
            {
                if (keyState.Value.Dirty)
                {
                    changes.Add(new StateValueChange()
                    {
                        Path = GetPathFor(keyState.Key),
                        BeforeValue = ByteString.CopyFrom(keyState.Value.InitialValue ?? new byte[0]),
                        AfterValue = ByteString.CopyFrom(keyState.Value.CurrentValue ?? new byte[0])
                    });
                }
            }

            foreach (var dp in _children)
            {
                changes.AddRange(dp.GetValueChanges());
            }

            return changes;
        }


        public void ClearTentativeCache()
        {
            _tentativeCache.Clear();
            foreach (var dp in _children)
            {
                dp.ClearTentativeCache();
            }
        }
    }
}