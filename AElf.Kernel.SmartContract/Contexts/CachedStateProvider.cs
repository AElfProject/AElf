using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Kernel.SmartContract.Contexts
{
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

        public Dictionary<StatePath, StateCache> Cache { get; set; } = new Dictionary<StatePath, StateCache>();

        public async Task<byte[]> GetAsync(StatePath path)
        {
            if (Cache.TryGetValue(path, out var c))
            {
                return c.CurrentValue;
            }

            var bytes = await _inner.GetAsync(path);
            Cache[path] = new StateCache(bytes);
            return bytes;
        }
    }
}