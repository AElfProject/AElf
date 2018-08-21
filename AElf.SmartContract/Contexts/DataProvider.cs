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
   
        public IEnumerable<StateValueChange> GetValueChanges()
        {
            var changes = new List<StateValueChange>();
            foreach (var keyState in StateCache)
            {
                changes.Add(new StateValueChange
                {
                    Path = keyState.Key,
                    CurrentValue = ByteString.CopyFrom(keyState.Value.CurrentValue ?? new byte[0])
                });
            }

            return changes;
        }

        /// <summary>
        /// DataPath - StateCache
        /// </summary>
        public Dictionary<DataPath, StateCache> StateCache { get; set; }

        public void ClearCache()
        {
            StateCache.Clear();
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
            return new DataProvider(_dataPath, _stateDictator, _layer++, dataProviderKey)
            {
                StateCache = StateCache
            };
        }

        /// <inheritdoc />
        public async Task<byte[]> GetAsync(Hash keyHash)
        {
            var data = (await GetStateAsync(keyHash)).CurrentValue;
            if (data == null)
            {
                Console.WriteLine("Failed to get data via DataProvider");
                data = new byte[0];
            }

            return data;
        }

        public async Task SetAsync(Hash keyHash, byte[] obj)
        {
            _dataPath.SetDataProvider(GetHash()).SetDataKey(keyHash);

            await _stateDictator.SetHashAsync(_dataPath.ResourcePathHash, _dataPath.ResourcePointerHash);
            
            var state = await GetStateAsync(keyHash);
            state.CurrentValue = obj;
        }

        public Hash GetPathFor(Hash keyHash)
        {
            return ((Hash)GetHash().CalculateHashWith(keyHash)).OfType(HashType.ResourcePath);
        }
        
        private async Task<StateCache> GetStateAsync(Hash keyHash)
        {
            _dataPath.SetDataProvider(GetHash()).SetDataKey(keyHash);
            
            if (StateCache.TryGetValue(_dataPath, out var state))
                return state;

            state = new StateCache(await GetDataAsync(keyHash));
            StateCache.Add(_dataPath, state);

            return state;
        }

        /// <summary>
        /// Get data from database.
        /// </summary>
        /// <param name="keyHash"></param>
        /// <returns></returns>
        public async Task<byte[]> GetDataAsync(Hash keyHash)
        {
            //Get resource pointer.
            var pointerHash = await _stateDictator.GetHashAsync(_dataPath.ResourcePathHash);
            
            //Use resource pointer get data.
            return await _stateDictator.GetDataAsync(pointerHash);
        }
    }
}