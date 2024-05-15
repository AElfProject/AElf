using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel.SmartContract;

public class CachedStateProvider : IScopedStateProvider
{
    private readonly IScopedStateProvider _inner;

    public CachedStateProvider(IScopedStateProvider inner)
    {
        _inner = inner;
    }

    public Address ContractAddress => _inner.ContractAddress;

    public IStateCache Cache { get; set; } = new InMemoryStateCache();

    public byte[] Get(StatePath path)
    {
        var scoped = new ScopedStatePath
        {
            Address = ContractAddress,
            Path = path
        };

        if (Cache.TryGetValue(scoped, out var value))
        {
            return value;
        }

        var bytes = _inner.Get(path);

        Cache[scoped] = bytes;

        return bytes;
    }

    private class InMemoryStateCache : IStateCache
    {
        private readonly Dictionary<ScopedStatePath, byte[]> _data = new();

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