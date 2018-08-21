using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

// ReSharper disable once CheckNamespace
namespace AElf.SmartContract
{
    public class DataProvider : IDataProvider
    {
        private readonly DataPath _dataPath;
        private readonly IStateDictator _stateDictator;
        private int _layer;
        
        private readonly List<DataProvider> _children = new List<DataProvider>();
        
        /// <summary>
        /// Key hash - State
        /// </summary>
        private Dictionary<Hash, StateCache> _caches = new Dictionary<Hash, StateCache>();

        public IEnumerable<StateValueChange> GetValueChanges()
        {
            var changes = new List<StateValueChange>();
            foreach (var keyState in _caches)
            {
                if (keyState.Value.Dirty)
                {
                    changes.Add(new StateValueChange
                    {
                        Path = _dataPath.SetDataProvider(GetHash()).SetDataKey(keyState.Key),
                        CurrentValue = ByteString.CopyFrom(keyState.Value.CurrentValue ?? new byte[0])
                    });
                }
            }

            foreach (var dp in _children)
            {
                changes.AddRange(dp.GetValueChanges());
            }

            return changes;
        }

        /// <summary>
        /// Key hash - StateCache
        /// </summary>
        public Dictionary<Hash, StateCache> StateCache
        {
            get => _caches;
            set
            {
                _caches = value;
                foreach (var dataProvider in _children)
                {
                    dataProvider.StateCache = value;
                }
            }
        }

        public void ClearCache()
        {
            _caches.Clear();
            foreach (var dp in _children)
            {
                dp.ClearCache();
            }
        }

        /// <summary>
        /// To dictinct DataProviders of same account and same level.
        /// Using a string value is just a choise, actually we can use any type of value, even integer.
        /// </summary>
        private readonly string _dataProviderKey;

        public DataProvider(DataPath dataPath, IStateDictator stateDictator, int layer = 0,
            string dataProviderKey = "")
        {
            _stateDictator = stateDictator;
            _dataProviderKey = dataProviderKey;
            _dataPath = dataPath;
            _layer = layer;
        }

        private Hash GetHash()
        {
            return new Int32Value {Value = _layer}.CalculateHashWith(_dataProviderKey);
        }

        /// <inheritdoc />
        /// <summary>
        /// Get a child DataProvider.
        /// </summary>
        /// <param name="dataProviderKey">child DataProvider's name</param>
        /// <returns></returns>
        public IDataProvider GetDataProvider(string dataProviderKey)
        {
            var dp = new DataProvider(_dataPath, _stateDictator, _layer++, dataProviderKey)
            {
                StateCache = StateCache
            };
            _children.Add(dp);
            return dp;
        }

        /// <inheritdoc />
        public async Task<byte[]> GetAsync(Hash keyHash)
        {
            return (await GetStateAsync(keyHash)).CurrentValue ?? new byte[0];
        }

        public async Task SetAsync(Hash keyHash, byte[] obj)
        {
            var state = await GetStateAsync(keyHash);
            state.CurrentValue = obj;
        }

        public Hash GetPathFor(Hash keyHash)
        {
            Console.WriteLine($"DataProvider: {GetHash()}");
            Console.WriteLine($"KeyHash: {keyHash.ToHex()}");
            return ((Hash)GetHash().CalculateHashWith(keyHash)).OfType(HashType.ResourcePath);
        }
        
        private async Task<StateCache> GetStateAsync(Hash keyHash)
        {
            if (_caches.TryGetValue(keyHash, out var state)) 
                return state;
            
            if (!StateCache.TryGetValue(GetPathFor(keyHash), out state))
            {
                state = new StateCache(await GetDataAsync(keyHash));
                StateCache.Add(GetPathFor(keyHash), state);
            }
            
            _caches.Add(keyHash, state);

            return state;
        }

        public async Task<byte[]> GetDataAsync(Hash keyHash)
        {
            //Get resource pointer.
            var pointerHash = await _stateDictator.GetHashAsync(GetPathFor(keyHash));
            
            //Use resource pointer get data.
            return await _stateDictator.GetDataAsync(pointerHash);
        }
    }
}