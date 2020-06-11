using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Domain
{
    public class TieredStateCache : IStateCache
    {
        private IStateCache _parent = new NullStateCache();
        private Dictionary<ScopedStatePath, byte[]> _originalValues = new Dictionary<ScopedStatePath, byte[]>();
        private Dictionary<string, byte[]> _currentValues = new Dictionary<string, byte[]>();

        public TieredStateCache() : this(new NullStateCache())
        {
        }

        public TieredStateCache(IStateCache parent)
        {
            if (parent != null)
            {
                _parent = parent;
            }
        }

        public bool TryGetValue(ScopedStatePath key, out byte[] value)
        {
            var originalFound = TryGetOriginalValue(key, out value);

            var currentFound = _currentValues.TryGetValue(key.ToStateKey(), out var currentValue);
            if (currentFound)
            {
                value = currentValue;
            }

            return originalFound || currentFound;
        }

        public byte[] this[ScopedStatePath key]
        {
            get => TryGetValue(key, out var value) ? value : null;
            set
            {
                // The setter is only used in StateProvider, changes have to be updated after execution
                // by calling Update
                _originalValues[key] = value;
                _parent[key] = value;
            }
        }

        public void Update(IEnumerable<TransactionExecutingStateSet> stateSets)
        {
            foreach (var stateSet in stateSets)
            {
                var changes = stateSet.Writes;
                foreach (var change in changes)
                {
                    _currentValues[change.Key] = change.Value.ToByteArray();
                }

                var deletes = stateSet.Deletes;
                foreach (var delete in deletes)
                {
                    _currentValues[delete.Key] = null;
                }
            }
        }

        private bool TryGetOriginalValue(ScopedStatePath path, out byte[] value)
        {
            if (_originalValues.TryGetValue(path, out value))
            {
                return true;
            }

            if (_parent.TryGetValue(path, out value))
            {
                _originalValues[path] = value;
                return true;
            }

            return false;
        }
    }
}