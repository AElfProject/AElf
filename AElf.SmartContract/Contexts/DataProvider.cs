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

        public int Layer { get; private set; }

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
            Layer = layer;
            _dataPath = dataPath.Clone();

            _dataPath.SetDataProvider(GetHash());
            Console.WriteLine("Layer: " + layer);
            Console.WriteLine("DP Hash: " + GetHash().ToHex());
        }

        private Hash GetHash()
        {
            return new StringValue {Value = _dataProviderKey}.CalculateHashWith(Layer);
        }

        /// <inheritdoc />
        /// <summary>
        /// Get a child DataProvider.
        /// </summary>
        /// <param name="dataProviderKey">child DataProvider's name</param>
        /// <returns></returns>
        public IDataProvider GetDataProvider(string dataProviderKey)
        {
            Layer++;
            return new DataProvider(_dataPath, _stateDictator, Layer, dataProviderKey)
            {
                StateCache = StateCache
            };
        }

        public async Task<byte[]> GetAsync<T>(Hash keyHash) where T : IMessage, new()
        {
            Console.WriteLine("Key Hash: " + keyHash.ToHex());
            return GetStateAsync(keyHash)?.CurrentValue ?? (await GetDataAsync<T>(keyHash))?.ToByteArray();
        }

        public async Task SetAsync<T>(Hash keyHash, byte[] obj) where T : IMessage, new()
        {
            Console.WriteLine("Key Hash: " + keyHash.ToHex());
            var dataPath = _dataPath.Clone();
            dataPath.SetDataKey(keyHash);
            if (!dataPath.AreYouOk())
            {
                throw new InvalidOperationException("DataPath: I'm not OK.");
            }
            
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
        
        private StateCache GetStateAsync(Hash keyHash)
        {
            var dataPath = _dataPath.Clone();
            dataPath.SetDataKey(keyHash);
            if (!dataPath.AreYouOk())
            {
                throw new InvalidOperationException("DataPath: I'm not OK.");
            }

            Console.WriteLine("Key:" + _dataProviderKey);
            Console.WriteLine($"Try to get path {dataPath.ResourcePathHash.ToHex()} from state cache.");
            if (StateCache.TryGetValue(dataPath, out var state))
            {
                return state;
            }
            
            Console.WriteLine("Failed.");
            return null;;
        }

        /// <inheritdoc />
        /// <summary>
        /// Directly set data to database instead of push it to cache.
        /// </summary>
        /// <param name="keyHash"></param>
        /// <param name="obj"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="T:System.InvalidOperationException">If the DataPath is not ready.</exception>
        public async Task SetDataAsync<T>(Hash keyHash, T obj) where T : IMessage, new()
        {
            var dataPath = _dataPath.Clone();
            dataPath.SetDataKey(keyHash);
            if (!dataPath.AreYouOk())
            {
                throw new InvalidOperationException("DataPath: I'm not OK.");
            }
            
            //Directly set to database.
            await _stateDictator.SetDataAsync(dataPath.ResourcePointerHash, obj);
            //Set path hash - pointer hash.
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
            dataPath.SetDataKey(keyHash);
            if (!dataPath.AreYouOk())
            {
                throw new InvalidOperationException("DataPath: I'm not OK.");
            }
            
            Console.WriteLine($"Try to get {dataPath.ResourcePathHash.ToHex()} from database.");
            //Get resource pointer.
            var pointerHash = await _stateDictator.GetHashAsync(dataPath.ResourcePathHash);
            if (pointerHash == null)
            {
                Console.WriteLine("But failed.");
                return default(T);
            }
            
            Console.WriteLine($"pointer hash: {pointerHash.ToHex()}");
            
            return await _stateDictator.GetDataAsync<T>(pointerHash);
        }
    }
}