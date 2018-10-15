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
    public class NewDataProvider : IDataProvider
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
        private Dictionary<string, StateChange> _cache = new Dictionary<string, StateChange>();
        private readonly List<NewDataProvider> _children = new List<NewDataProvider>();

        private NewDataProvider(Hash chainId, Address contractAddress, IReadOnlyList<string> path)
        {
            ChainId = chainId;
            ContractAddress = contractAddress;
            Path = path;
        }

        public static NewDataProvider GetRootDataProvider(Hash chainId, Address contractAddress)
        {
            return new NewDataProvider(chainId, contractAddress, new string[0]);
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

        public NewDataProvider GetChild(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("String is null or empty.", nameof(name));
            }

            var path = new List<string>(Path) {"", name};
            var child = new NewDataProvider(ChainId, ContractAddress, path) {StateStore = _stateStore};
            _children.Add(child);
            return child;
        }

        public void ClearCache()
        {
            _cache = new Dictionary<string, StateChange>();
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
            _cache[key] = StateChange.Create(bytes);
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

        public Dictionary<StatePath, StateChange> GetChanges()
        {
            var d = new Dictionary<StatePath, StateChange>();
            foreach (var kv in _cache)
            {
                if (kv.Value.IsDirty)
                {
                    var sp = GetStatePathFor(kv.Key);
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

        public async Task SetDataAsync<T>(Hash keyHash, T obj) where T : IMessage, new()
        {
            Console.WriteLine($"Setting data {keyHash.DumpHex()}");
            var sp = GetStatePathFor(keyHash.DumpHex(), true);
            await _stateStore.SetAsync(sp, obj.ToByteArray());
        }

        public async Task<byte[]> GetAsync<T>(Hash keyHash) where T : IMessage, new()
        {
            return await GetAsync(keyHash.DumpHex());
        }

        public async Task SetAsync<T>(Hash keyHash, byte[] obj) where T : IMessage, new()
        {
            Console.WriteLine($"Setting {keyHash.DumpHex()}");
            await SetAsync(keyHash.DumpHex(), obj);
        }

        public IDataProvider GetDataProvider(string dataProviderKey)
        {
            return GetChild(dataProviderKey);
        }

        public IEnumerable<StateValueChange> GetValueChanges()
        {
            var changes = new List<StateValueChange>();
            foreach (var kv in GetChanges())
            {
                var c = new StateValueChange()
                {
                    CurrentValue = kv.Value.CurrentValue,
                    Path = new DataPath()
                    {
                        ChainId = ChainId,
                        ContractAddress = ContractAddress,
                        KeyHash = Hash.FromMessage(kv.Key),
                        StatePath = kv.Key
                    }
                };
                changes.Add(c);
            }

            return changes;
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