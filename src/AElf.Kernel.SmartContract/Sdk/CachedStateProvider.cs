using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.SmartContract.Sdk
{
    public class CachedStateProvider : IScopedStateProvider
    {
        public Address ContractAddress => _inner.ContractAddress;

        private readonly IScopedStateProvider _inner;

        public CachedStateProvider(IScopedStateProvider inner)
        {
            _inner = inner;
        }

        public IStateCache Cache { get; set; } = new InMemoryStateCache();

        public async Task<byte[]> GetAsync(StatePath path)
        {
            var scoped = new ScopedStatePath()
            {
                Address = ContractAddress,
                Path = path
            };

            if (Cache.TryGetValue(scoped, out var value))
            {
                return value;
            }

            var bytes = await _inner.GetAsync(path);

            Cache[scoped] = bytes;

            return bytes;
        }

        private class InMemoryStateCache : IStateCache
        {
            private readonly Dictionary<ScopedStatePath, byte[]> _data =
                new Dictionary<ScopedStatePath, byte[]>();

            //TODO: Add TryGetValue and this[StatePath key] cases [Case]
            public bool TryGetValue(ScopedStatePath key, out byte[] value)
            {
                return _data.TryGetValue(key, out value);
            }

            public byte[] this[ScopedStatePath key]
            {
                get => TryGetValue(key, out var value) ? value : null;
                set => _data[key] = value;
            }
        }
    }
}