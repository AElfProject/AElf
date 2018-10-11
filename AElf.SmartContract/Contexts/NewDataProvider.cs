using System;
using System.Collections.Generic;
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
            get
            {
                return _stateStore;
            }
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

        private StatePath GetStatePathFor(string key, bool full=false)
        {
            var sp = new StatePath();
            if (full)
            {
                sp.ChainId = ChainId;
                sp.ContractAddress = ContractAddress;
            }
            sp.Path.AddRange(Path);
            sp.Path.Add("");
            sp.Path.Add(key);
            return sp;
        }
        
        public NewDataProvider GetChild(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("String is null or empty.", nameof(name));
            }

            var path = new List<string>(Path) {name};
            var child = new NewDataProvider(ChainId, ContractAddress, path);
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

            return await _stateStore.GetAsync(GetStatePathFor(key, true));
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
                c = new StateChange();
                _cache[key] = c;
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
    }
}