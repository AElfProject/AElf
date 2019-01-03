using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using AElf.Kernel;
using Google.Protobuf;

namespace AElf.Sdk.CSharp.State
{
    public class MappedStateBase : StateBase
    {
        internal StatePath GetSubStatePath(string key)
        {
            var statePath = this.Path.Clone();
            statePath.Path.Add(ByteString.CopyFromUtf8(key));
            return statePath;
        }
    }

    public class MappedState<TKey, TEntity> : MappedStateBase
    {
        internal class ValuePair
        {
            internal TEntity OriginalValue;
            internal TEntity Value;
        }

        internal Dictionary<TKey, ValuePair> Cache = new Dictionary<TKey, ValuePair>();

        public TEntity this[TKey key]
        {
            get
            {
                if (!Cache.TryGetValue(key, out var valuePair))
                {
                    valuePair = LoadKey(key);
                }

                return valuePair.Value;
            }
            set
            {
                if (!Cache.TryGetValue(key, out var valuePair))
                {
                    valuePair = LoadKey(key);
                    Cache[key] = valuePair;
                }

                valuePair.Value = value;
            }
        }

        internal override void Clear()
        {
            Cache = new Dictionary<TKey, ValuePair>();
        }

        internal override Dictionary<StatePath, StateValue> GetChanges()
        {
            var dict = new Dictionary<StatePath, StateValue>();
            foreach (var kv in Cache)
            {
                if (!Equals(kv.Value.OriginalValue, kv.Value.Value))
                {
                    dict[GetSubStatePath(kv.Key.ToString())] = new StateValue()
                    {
                        CurrentValue = ByteString.CopyFrom(SerializationHelpers.Serialize(kv.Value.Value)),
                        OriginalValue = ByteString.CopyFrom(SerializationHelpers.Serialize(kv.Value.OriginalValue))
                    };
                }
            }

            return dict;
        }

        private ValuePair LoadKey(TKey key)
        {
            var path = GetSubStatePath(key.ToString());
            var bytes = Context.StateManager.GetAsync(path).Result;
            var value = SerializationHelpers.Deserialize<TEntity>(bytes);

            return new ValuePair()
            {
                OriginalValue = value,
                Value = value
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
                        Context = this.Context,
                        Path = GetSubStatePath(key1.ToString())
                    };
                    Cache[key1] = child;
                }

                return child;
            }
        }

        internal override void Clear()
        {
            Cache = new Dictionary<TKey1, MappedState<TKey2, TEntity>>();
        }

        internal override Dictionary<StatePath, StateValue> GetChanges()
        {
            var dict = new Dictionary<StatePath, StateValue>();
            foreach (var kv in Cache)
            {
                foreach (var kv1 in kv.Value.GetChanges())
                {
                    dict[kv1.Key] = kv1.Value;
                }
            }

            return dict;
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
                        Context = this.Context,
                        Path = GetSubStatePath(key1.ToString())
                    };
                    Cache[key1] = child;
                }

                return child;
            }
        }

        internal override void Clear()
        {
            Cache = new Dictionary<TKey1, MappedState<TKey2, TKey3, TEntity>>();
        }

        internal override Dictionary<StatePath, StateValue> GetChanges()
        {
            var dict = new Dictionary<StatePath, StateValue>();
            foreach (var kv in Cache)
            {
                foreach (var kv1 in kv.Value.GetChanges())
                {
                    dict[kv1.Key] = kv1.Value;
                }
            }

            return dict;
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
                        Context = this.Context,
                        Path = GetSubStatePath(key1.ToString())
                    };
                    Cache[key1] = child;
                }

                return child;
            }
        }

        internal override void Clear()
        {
            Cache = new Dictionary<TKey1, MappedState<TKey2, TKey3, TKey4, TEntity>>();
        }

        internal override Dictionary<StatePath, StateValue> GetChanges()
        {
            var dict = new Dictionary<StatePath, StateValue>();
            foreach (var kv in Cache)
            {
                foreach (var kv1 in kv.Value.GetChanges())
                {
                    dict[kv1.Key] = kv1.Value;
                }
            }

            return dict;
        }
    }
}