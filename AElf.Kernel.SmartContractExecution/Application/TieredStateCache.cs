using System.Collections.Generic;
using AElf.Kernel.SmartContract.Contexts;
using Google.Protobuf;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class TieredStateCache : IStateCache
    {
        private IStateCache _parent;
        private Dictionary<StatePath, byte[]> _originalValues = new Dictionary<StatePath, byte[]>();
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

        public bool TryGetValue(StatePath key, out byte[] value)
        {
            // if the original value doesn't exist, then the state is not in the cache
            if (!TryGetOriginalValue(key, out value))
            {
                return false;
            }

            // the original value was found, check if the value is changed
            if (_currentValues.TryGetValue(key.ToStateKey(), out var currentValue))
            {
                value = currentValue;
            }

            return true;
        }

        public byte[] this[StatePath key]
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

        public void Update(IEnumerable<KeyValuePair<string, byte[]>> changes)
        {
            foreach (var change in changes)
            {
                _currentValues[change.Key] = change.Value;
            }
        }

        private bool TryGetOriginalValue(StatePath path, out byte[] value)
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