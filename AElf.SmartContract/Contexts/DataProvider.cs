using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using AElf.Common;
using AElf.Kernel.Storages;

// ReSharper disable once CheckNamespace
namespace AElf.SmartContract
{
    public class DataProvider : IDataProvider
    {
        private IStateStore _stateStore;

        public IStateStore StateStore
        {
            get { return _stateStore; }
            set
            {
                _stateStore = value;
                foreach (var child in _children)
                {
                    child.StateStore = value;
                }
            }
        }

        public Hash ChainId { get; }
        public Address ContractAddress { get; }
        public IReadOnlyList<string> Path { get; }
        private Dictionary<string, StateValue> _cache = new Dictionary<string, StateValue>();
        private readonly List<DataProvider> _children = new List<DataProvider>();

        private DataProvider(Hash chainId, Address contractAddress, IReadOnlyList<string> path)
        {
            ChainId = chainId;
            ContractAddress = contractAddress;
            Path = path;
        }

        public static DataProvider GetRootDataProvider(Hash chainId, Address contractAddress)
        {
            return new DataProvider(chainId, contractAddress, new string[0]);
        }

        private StatePath GetStatePathFor(string key, bool full = false)
        {
            var sp = new StatePath();
            if (full)
            {
                sp.ChainId = ChainId;
                sp.ContractAddress = ContractAddress;
            }

            sp.Path.AddRange(Path);
            sp.Path.Add(key);
            return sp;
        }

        public DataProvider GetChild(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("String is null or empty.", nameof(name));
            }

            var path = new List<string>(Path) {"", name};
            var child = new DataProvider(ChainId, ContractAddress, path) {StateStore = _stateStore};
            _children.Add(child);
            return child;
        }

        public void ClearCache()
        {
            _cache = new Dictionary<string, StateValue>();
            foreach (var child in _children)
            {
                child.ClearCache();
            }
            StateCache = new Dictionary<DataPath, StateCache>();
        }

        public async Task<byte[]> GetAsync(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (_cache.TryGetValue(key, out var value))
            {
                return value.Get();
            }

            var bytes = await _stateStore.GetAsync(GetStatePathFor(key, true));
            _cache[key] = StateValue.Create(bytes);
            return bytes;
        }

        public async Task SetAsync(string key, byte[] value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (!_cache.TryGetValue(key, out var c))
            {
                await GetAsync(key);
                if (!_cache.TryGetValue(key, out c))
                {
                    // This should not happen
                    throw new Exception("Error initializing cache.");
                }
            }
            c.Set(value);
            await Task.CompletedTask;
        }

        public Dictionary<StatePath, StateValue> GetChanges()
        {
            var d = new Dictionary<StatePath, StateValue>();
            foreach (var kv in _cache)
            {
                if (kv.Value.IsDirty)
                {
                    var sp = GetStatePathFor(kv.Key, true);
                    d[sp] = kv.Value;
                }
            }

            foreach (var child in _children)
            {
                foreach (var kv in child.GetChanges())
                {
                    d.Add(kv.Key, kv.Value);
                }
            }

            return d;
        }

        public override string ToString()
        {
            return $"/{ChainId.DumpHex()}/{ContractAddress.DumpHex()}/{string.Join("/", Path)}";
        }

        #region temporary conform to interface

        public async Task<byte[]> GetAsync<T>(Hash keyHash) where T : IMessage, new()
        {
            return await GetAsync(keyHash.DumpHex());
        }

        public async Task SetAsync<T>(Hash keyHash, byte[] obj) where T : IMessage, new()
        {
            await SetAsync(keyHash.DumpHex(), obj);
        }

        public IDataProvider GetDataProvider(string dataProviderKey)
        {
            return GetChild(dataProviderKey);
        }

        private Dictionary<DataPath, StateCache> _stateCache = new Dictionary<DataPath, StateCache>();

        public Dictionary<DataPath, StateCache> StateCache
        {
            get => _stateCache;
            set
            {
                _stateCache = value;
                foreach (var c in _children)
                {
                    c.StateCache = value;
                }
            }
        }

        #endregion
    }
}