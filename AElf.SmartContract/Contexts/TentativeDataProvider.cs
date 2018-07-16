using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf;
using AElf.Kernel;

namespace AElf.SmartContract
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
            get => _currentValue;
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

        private readonly Dictionary<Hash, StateCache> _tentativeCache = new Dictionary<Hash, StateCache>();

        private async Task<StateCache> GetStateAsync(Hash keyHash)
        {
            if (!_tentativeCache.TryGetValue(keyHash, out var state))
            {
                state = new StateCache(await _dataProvider.GetAsync(keyHash));
                _tentativeCache.Add(keyHash, state);
            }

            return state;
        }

        public TentativeDataProvider(IDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        public IDataProvider GetDataProvider(string name)
        {
            var dp = new TentativeDataProvider(_dataProvider.GetDataProvider(name));
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