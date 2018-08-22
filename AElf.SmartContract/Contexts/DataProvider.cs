using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Enum = System.Enum;

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

        /// <inheritdoc />
        /// <summary>
        /// DataPath - StateCache
        /// </summary>
        public Dictionary<DataPath, StateCache> StateCache { get; set; } = new Dictionary<DataPath, StateCache>();

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
            return new DataProvider(_dataPath.Clone(), _stateDictator, _layer++, dataProviderKey)
            {
                StateCache = StateCache
            };
        }

        public async Task<byte[]> GetAsync<T>(Hash keyHash) where T : IMessage, new()
        {
            return GetStateAsync(keyHash)?.CurrentValue ?? (await GetDataAsync<T>(keyHash))?.ToByteArray();
        }

        public async Task SetAsync<T>(Hash keyHash, byte[] obj) where T : IMessage, new()
        {
            var dataPath = _dataPath.Clone();
            dataPath.SetDataProvider(GetHash()).SetDataKey(keyHash);
            
            if (!Enum.TryParse<Kernel.Storages.Types>(typeof(T).Name, out var typeIndex))
            {
                throw new Exception($"Not Supported Data Type, {typeof(T).Name}.");
            }

            dataPath.Type = (DataPath.Types) (uint) typeIndex;
            
            await _stateDictator.SetHashAsync(dataPath.ResourcePathHash, dataPath.ResourcePointerHash);
            var state = GetStateAsync(keyHash);

            if (state == null)
            {
                state = new StateCache((await GetDataAsync<T>(keyHash))?.ToByteArray());
                StateCache.Add(dataPath, state);
            }
            
            state.CurrentValue = obj;
        }

        public Hash GetPathFor(Hash keyHash)
        {
            return ((Hash)GetHash().CalculateHashWith(keyHash)).OfType(HashType.ResourcePath);
        }
        
        private StateCache GetStateAsync(Hash keyHash)
        {
            var dataPath = _dataPath.Clone();
            dataPath.SetDataProvider(GetHash()).SetDataKey(keyHash);
            Console.WriteLine($"Try to get {dataPath.ResourcePathHash.ToHex()} from state cache.");
            return StateCache.TryGetValue(dataPath, out var state) ? state : null;
        }

        public async Task SetDataAsync<T>(Hash keyHash, T obj) where T : IMessage, new()
        {
            var dataPath = _dataPath.Clone();
            dataPath.SetDataProvider(GetHash()).SetDataKey(keyHash);

            await _stateDictator.SetDataAsync(dataPath.ResourcePointerHash, obj);
            
            await _stateDictator.SetHashAsync(dataPath.ResourcePathHash, dataPath.ResourcePointerHash);
        }
        
        /// <summary>
        /// Get data from database.
        /// </summary>
        /// <param name="keyHash"></param>
        /// <returns></returns>
        public async Task<T> GetDataAsync<T>(Hash keyHash) where T : IMessage, new()
        {
            var dataPath = _dataPath.Clone();
            dataPath.SetDataProvider(GetHash()).SetDataKey(keyHash);

            Console.WriteLine($"Try to get {dataPath.ResourcePathHash.ToHex()} from database.");

            //Get resource pointer.
            var pointerHash = await _stateDictator.GetHashAsync(dataPath.ResourcePathHash);

            if (pointerHash == null)
            {
                return default(T);
            }
            
            Console.WriteLine($"pointer hash: {pointerHash.ToHex()}");
            
            //Use resource pointer get data.
            return await _stateDictator.GetDataAsync<T>(pointerHash);
        }
    }
}