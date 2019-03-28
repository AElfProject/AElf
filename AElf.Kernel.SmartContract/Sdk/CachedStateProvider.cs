using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.SmartContract.Sdk
{
    public class CachedStateProvider : IStateProvider
    {
        private readonly IStateProvider _inner;

        public CachedStateProvider(IStateProvider inner)
        {
            _inner = inner;
        }

        public IStateCache Cache { get; set; } = new InMemoryStateCache();

        public async Task<byte[]> GetAsync(StatePath path)
        {
            if (Cache.TryGetValue(path, out var value)) return value;

            var bytes = await _inner.GetAsync(path);

            Cache[path] = bytes;

            return bytes;
        }

        private class InMemoryStateCache : IStateCache
        {
            private readonly Dictionary<StatePath, byte[]> _data =
                new Dictionary<StatePath, byte[]>();

            //TODO: Add TryGetValue and this[StatePath key] cases [Case]
            public bool TryGetValue(StatePath key, out byte[] value)
            {
                return _data.TryGetValue(key, out value);
            }

            public byte[] this[StatePath key]
            {
                get => TryGetValue(key, out var value) ? value : null;
                set => _data[key] = value;
            }
        }
    }
}