using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
        public IReadOnlyList<ByteString> Path { get; }

        /// <summary>
        /// Used to cache data changes for this DataProvider
        /// </summary>
        private Dictionary<string, StateValue> _localCache = new Dictionary<string, StateValue>();

        private readonly List<DataProvider> _children = new List<DataProvider>();

        private DataProvider(Hash chainId, Address contractAddress, IReadOnlyList<ByteString> path)
        {
            ChainId = chainId;
            ContractAddress = contractAddress;
            Path = path;
        }

        public static DataProvider GetRootDataProvider(Hash chainId, Address contractAddress)
        {
            return new DataProvider(chainId, contractAddress, new ByteString[]
            {
                ByteString.CopyFrom(contractAddress.DumpByteArray())
            });
        }

        private StatePath GetStatePathFor(string key)
        {
            var sp = new StatePath();
            sp.Path.AddRange(Path);
            sp.Path.Add(ByteString.CopyFromUtf8(key));
            return sp;
        }

        public DataProvider GetChild(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("String is null or empty.", nameof(name));
            }

            var path = new List<ByteString>(Path) {ByteString.Empty, ByteString.CopyFromUtf8(name)};
            var child = new DataProvider(ChainId, ContractAddress, path) {StateStore = _stateStore};
            _children.Add(child);
            return child;
        }

        public void ClearCache()
        {
            _localCache = new Dictionary<string, StateValue>();
            foreach (var child in _children)
            {
                child.ClearCache();
            }

//            StateCache = new Dictionary<DataPath, StateCache>();
        }

        public async Task<byte[]> GetAsync(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (_localCache.TryGetValue(key, out var value))
            {
                return value.Get();
            }

            var path = GetStatePathFor(key);
            if (_stateCache.TryGetValue(path, out var cached))
            {
                return cached.CurrentValue;
            }

            var bytes = await _stateStore.GetAsync(path);
            _localCache[key] = StateValue.Create(bytes);
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

            if (!_localCache.TryGetValue(key, out var c))
            {
                await GetAsync(key);
                if (!_localCache.TryGetValue(key, out c))
                {
                    // This should not happen
                    throw new Exception("Error initializing cache.");
                }
            }

            c.Set(value);
            StateCache[GetStatePathFor(key)] = new StateCache(value);
            await Task.CompletedTask;
        }

        public Dictionary<StatePath, StateValue> GetChanges()
        {
            var d = new Dictionary<StatePath, StateValue>();
            foreach (var kv in _localCache)
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
            return $"/{ChainId.DumpBase58()}/{ContractAddress.GetFormatted()}/{string.Join("/", Path)}";
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

        private Dictionary<StatePath, StateCache> _stateCache = new Dictionary<StatePath, StateCache>();

        /// <summary>
        /// Used to cache state for current transaction tree (i.e. transaction with nested inline transactions),
        /// the cached values have not been committed to database yet, which will be done after the transaction tree
        /// finishes. 
        /// </summary>
        public Dictionary<StatePath, StateCache> StateCache
        {
            // TODO: This cache maybe moved to DataStore
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