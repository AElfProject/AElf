using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Kernel.SmartContract.Contexts
{
    public class InmemoryStateCache : IStateCache
    {
        private Dictionary<StatePath, byte[]> _data = new Dictionary<StatePath, byte[]>();

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

    public class CachedStateProvider : IStateProvider
    {
        public ITransactionContext TransactionContext
        {
            get => _inner.TransactionContext;
            set => _inner.TransactionContext = value;
        }

        private IStateProvider _inner;

        public CachedStateProvider(IStateProvider inner)
        {
            _inner = inner;
        }

        public IStateCache Cache { get; set; } = new InmemoryStateCache();

        public async Task<byte[]> GetAsync(StatePath path)
        {
            if (Cache.TryGetValue(path, out var value))
            {
                return value;
            }

            var bytes = await _inner.GetAsync(path);

            Cache[path] = bytes;

            return bytes;
        }
    }
}