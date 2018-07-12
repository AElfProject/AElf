using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf;
using System.Linq;

namespace AElf.Kernel
{
    internal class StateCache
    {
        private byte[] _currentValue;

        public StateCache(byte[] initialValue)
        {
            InitialValue = initialValue;
            _currentValue = initialValue;
        }

        public bool Dirty { get; private set; } = false;

        public byte[] InitialValue { get; }

        public byte[] CurrentValue
        {
            get { return _currentValue; }
            set
            {
                Dirty = true;
                _currentValue = value;
            }
        }


        public void SetValue(byte[] value)
        {
            Dirty = true;
            CurrentValue = value;
        }
    }

    public class TentativeDataProvider : ITentativeDataProvider
    {
        private IDataProvider _dataProvider;

        private List<TentativeDataProvider> _children = new List<TentativeDataProvider>();

        public Dictionary<Hash, byte[]> StateCache
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
        } //temporary solution to let data provider access actor's state cache

        private Dictionary<Hash, byte[]> _stateCache;

        private readonly Dictionary<Hash, StateCache> _tentativeCache = new Dictionary<Hash, StateCache>();

        private async Task<StateCache> GetStateAsync(Hash keyHash)
        {
            //Console.WriteLine($"Trying to get with cache of size {StateCache.Count}: " + string.Join(", ", StateCache.Select(kv=> $"[{kv.Key} : {kv.Value}]")));
            if (!_tentativeCache.TryGetValue(keyHash, out var state))
            {
                if (!StateCache.TryGetValue(GetPathFor(keyHash), out var rawData))
                {
                    //Console.WriteLine($"Can't find Key {GetPathFor(keyHash)} in cache");
                    state = new StateCache(await _dataProvider.GetAsync(keyHash));
                }
                else
                {
                    //Console.WriteLine($"Key {GetPathFor(keyHash)} hit cache");
                    state = new StateCache(rawData);
                }
                
                _tentativeCache.Add(keyHash, state);
            }

            return state;
        }

        public TentativeDataProvider(IDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
            StateCache = new Dictionary<Hash, byte[]>();
        }

        public IDataProvider GetDataProvider(string name)
        {
            var dp = new TentativeDataProvider(_dataProvider.GetDataProvider(name));
            dp.StateCache = StateCache; //temporary solution to let data provider access actor's state cache
            _children.Add(dp);
            return dp;
        }

        public async Task<Change> SetAsync(Hash keyHash, byte[] obj)
        {
            var state = await GetStateAsync(keyHash);
            state.CurrentValue = obj;

            return null;
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
        
        public void ClearCache()
        {
            _tentativeCache.Clear();
            foreach (var dp in _children)
            {
                dp.ClearCache();
            }
        }
    }
}