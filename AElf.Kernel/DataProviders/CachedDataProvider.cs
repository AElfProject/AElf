using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading.Tasks;
using AElf.Database;
using AElf.Kernel.Extensions;
using AElf.Kernel.Managers;
using AElf.Kernel;
using Google.Protobuf;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Libuv.Internal.Networking;
using Microsoft.Net.Http.Headers;

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

    public class CachedDataProvider : ICachedDataProvider
    {
        private IDataProvider _dataProvider;

        private List<CachedDataProvider> _children = new List<CachedDataProvider>();

        private readonly Dictionary<Hash, StateCache> _cache = new Dictionary<Hash, StateCache>();

        private bool _autoCommit;

        public bool AutoCommit
        {
            get => _autoCommit;
            set
            {
                _autoCommit = value;
                foreach (var dp in _children)
                {
                    dp.AutoCommit = value;
                }
            }
        }

        private async Task<StateCache> GetStateAsync(Hash keyHash)
        {
            if (!_cache.TryGetValue(keyHash, out var state))
            {
                state = new StateCache(await _dataProvider.GetAsync(keyHash));
                _cache.Add(keyHash, state);
            }

            return state;
        }

        public CachedDataProvider(IDataProvider dataProvider)
        {
            _dataProvider = dataProvider;
        }

        public IDataProvider GetDataProvider(string name)
        {
            var dp = new CachedDataProvider(_dataProvider.GetDataProvider(name))
            {
                AutoCommit = AutoCommit
            };
            _children.Add(dp);
            return dp;
        }

        public async Task<Change> SetAsync(Hash keyHash, byte[] obj)
        {
            var state = await GetStateAsync(keyHash);
            state.CurrentValue = obj;

            if (AutoCommit)
            {
                return await _dataProvider.SetAsync(keyHash, obj);
            }

            return null;
        }

        public async Task<byte[]> GetAsync(Hash keyHash)
        {
            return (await GetStateAsync(keyHash)).CurrentValue;
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
            foreach (var keyState in _cache)
            {
                if (keyState.Value.Dirty)
                {
                    changes.Add(new StateValueChange()
                    {
                        Path = GetPathFor(keyState.Key),
                        BeforeValue = ByteString.CopyFrom(keyState.Value.InitialValue??new byte[0]),
                        AfterValue = ByteString.CopyFrom(keyState.Value.CurrentValue??new byte[0])
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
            _cache.Clear();
            foreach (var dp in _children)
            {
                dp.ClearCache();
            }
        }
    }
}