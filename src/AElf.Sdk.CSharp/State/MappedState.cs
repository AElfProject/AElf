using System.Collections.Generic;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Sdk.CSharp.State
{
    public class MappedStateBase : StateBase
    {
        internal StatePath GetSubStatePath(string key)
        {
            var statePath = this.Path.Clone();
            statePath.Parts.Add(key);
            return statePath;
        }
    }

    /// <summary>
    /// Key-value pair data structure used for representing state in contracts.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TEntity">The type of the value.</typeparam>
    public class MappedState<TKey, TEntity> : MappedStateBase
    {
        internal class ValuePair
        {
            internal TEntity OriginalValue;
            internal TEntity Value;
            internal bool IsDeleted;
        }

        internal Dictionary<TKey, ValuePair> Cache = new Dictionary<TKey, ValuePair>();

        public TEntity this[TKey key]
        {
            get
            {
                if (!Cache.TryGetValue(key, out var valuePair))
                {
                    valuePair = LoadKey(key);
                    Cache[key] = valuePair;
                }

                return valuePair.IsDeleted ? SerializationHelper.Deserialize<TEntity>(null) : valuePair.Value;
            }
            set
            {
                if (!Cache.TryGetValue(key, out var valuePair))
                {
                    valuePair = LoadKey(key);
                    Cache[key] = valuePair;
                }

                valuePair.IsDeleted = false;
                valuePair.Value = value;
            }
        }

        public void Remove(TKey key)
        {
            if (!Cache.TryGetValue(key, out var valuePair))
            {
                valuePair = LoadKey(key);
                Cache[key] = valuePair;
            }

            valuePair.IsDeleted = true;
        }
        
        public void Set(TKey key, TEntity value)
        {
            var valuePair = new ValuePair
            {
                OriginalValue = default(TEntity),
                Value = default(TEntity)
            };
            Cache[key] = valuePair;
            valuePair.Value = value;
        }

        internal override void Clear()
        {
            Cache = new Dictionary<TKey, ValuePair>();
        }

        internal override TransactionExecutingStateSet GetChanges()
        {
            var stateSet = new TransactionExecutingStateSet();
            foreach (var kv in Cache)
            {
                var key = GetSubStatePath(kv.Key.ToString()).ToStateKey(Context.Self);
                if (kv.Value.IsDeleted)
                {
                    stateSet.Deletes[key] = true;
                }
                else if (!Equals(kv.Value.OriginalValue, kv.Value.Value))
                {
                    stateSet.Writes[key] = ByteString.CopyFrom(SerializationHelper.Serialize(kv.Value.Value));
                }

                stateSet.Reads[key] = true;
            }

            return stateSet;
        }

        private ValuePair LoadKey(TKey key)
        {
            var path = GetSubStatePath(key.ToString());
            var bytes = Provider.Get(path);
            var value = SerializationHelper.Deserialize<TEntity>(bytes);
            var originalValue = SerializationHelper.Deserialize<TEntity>(bytes);

            return new ValuePair
            {
                OriginalValue = originalValue,
                Value = value,
                IsDeleted = false
            };
        }
    }

    public class MappedState<TKey1, TKey2, TEntity> : MappedStateBase
    {
        internal Dictionary<TKey1, MappedState<TKey2, TEntity>> Cache =
            new Dictionary<TKey1, MappedState<TKey2, TEntity>>();

        public MappedState<TKey2, TEntity> this[TKey1 key1]
        {
            get
            {
                if (!Cache.TryGetValue(key1, out var child))
                {
                    child = new MappedState<TKey2, TEntity>()
                    {
                        Context = Context,
                        Path = GetSubStatePath(key1.ToString())
                    };
                    Cache[key1] = child;
                }

                return child;
            }
        }

        internal override void OnContextSet()
        {
            foreach (var v in Cache.Values)
            {
                v.Context = Context;
            }
        }

        internal override void Clear()
        {
            Cache = new Dictionary<TKey1, MappedState<TKey2, TEntity>>();
        }

        internal override TransactionExecutingStateSet GetChanges()
        {
            var stateSet = new TransactionExecutingStateSet();
            foreach (var kv in Cache)
            {
                var changes = kv.Value.GetChanges();
                foreach (var kv1 in changes.Deletes)
                {
                    stateSet.Deletes[kv1.Key] = kv1.Value;
                }
                
                foreach (var kv1 in changes.Writes)
                {
                    stateSet.Writes[kv1.Key] = kv1.Value;
                }

                foreach (var kv1 in changes.Reads)
                {
                    stateSet.Reads[kv1.Key] = kv1.Value;
                }
            }

            return stateSet;
        }
    }

    public class MappedState<TKey1, TKey2, TKey3, TEntity> : MappedStateBase
    {
        internal Dictionary<TKey1, MappedState<TKey2, TKey3, TEntity>> Cache =
            new Dictionary<TKey1, MappedState<TKey2, TKey3, TEntity>>();

        public MappedState<TKey2, TKey3, TEntity> this[TKey1 key1]
        {
            get
            {
                if (!Cache.TryGetValue(key1, out var child))
                {
                    child = new MappedState<TKey2, TKey3, TEntity>()
                    {
                        Context = Context,
                        Path = GetSubStatePath(key1.ToString())
                    };
                    Cache[key1] = child;
                }

                return child;
            }
        }

        internal override void OnContextSet()
        {
            foreach (var v in Cache.Values)
            {
                v.Context = Context;
            }
        }

        internal override void Clear()
        {
            Cache = new Dictionary<TKey1, MappedState<TKey2, TKey3, TEntity>>();
        }

        internal override TransactionExecutingStateSet GetChanges()
        {
            var stateSet = new TransactionExecutingStateSet();
            foreach (var kv in Cache)
            {
                var changes = kv.Value.GetChanges();
                foreach (var kv1 in changes.Deletes)
                {
                    stateSet.Deletes[kv1.Key] = kv1.Value;
                }
                
                foreach (var kv1 in changes.Writes)
                {
                    stateSet.Writes[kv1.Key] = kv1.Value;
                }

                foreach (var kv1 in changes.Reads)
                {
                    stateSet.Reads[kv1.Key] = kv1.Value;
                }
            }

            return stateSet;
        }
    }

    public class MappedState<TKey1, TKey2, TKey3, TKey4, TEntity> : MappedStateBase
    {
        internal Dictionary<TKey1, MappedState<TKey2, TKey3, TKey4, TEntity>> Cache =
            new Dictionary<TKey1, MappedState<TKey2, TKey3, TKey4, TEntity>>();

        public MappedState<TKey2, TKey3, TKey4, TEntity> this[TKey1 key1]
        {
            get
            {
                if (!Cache.TryGetValue(key1, out var child))
                {
                    child = new MappedState<TKey2, TKey3, TKey4, TEntity>()
                    {
                        Context = Context,
                        Path = GetSubStatePath(key1.ToString())
                    };
                    Cache[key1] = child;
                }

                return child;
            }
        }

        internal override void OnContextSet()
        {
            foreach (var v in Cache.Values)
            {
                v.Context = Context;
            }
        }

        internal override void Clear()
        {
            Cache = new Dictionary<TKey1, MappedState<TKey2, TKey3, TKey4, TEntity>>();
        }

        internal override TransactionExecutingStateSet GetChanges()
        {
            var stateSet = new TransactionExecutingStateSet();
            foreach (var kv in Cache)
            {
                var changes = kv.Value.GetChanges();
                foreach (var kv1 in changes.Deletes)
                {
                    stateSet.Deletes[kv1.Key] = kv1.Value;
                }
                
                foreach (var kv1 in changes.Writes)
                {
                    stateSet.Writes[kv1.Key] = kv1.Value;
                }

                foreach (var kv1 in changes.Reads)
                {
                    stateSet.Reads[kv1.Key] = kv1.Value;
                }
            }

            return stateSet;
        }
    }
}