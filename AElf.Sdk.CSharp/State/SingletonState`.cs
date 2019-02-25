using System;
using System.Collections.Generic;
using AElf.Kernel;
using Google.Protobuf;

namespace AElf.Sdk.CSharp.State
{
    public class SingletonState : StateBase
    {
    }

    public class SingletonState<TEntity> : SingletonState
    {
        internal bool Loaded = false;
        internal bool Modified => Equals(_originalValue, _value);

        private TEntity _originalValue;
        private TEntity _value;

        public TEntity Value
        {
            get
            {
                if (!Loaded)
                {
                    Load();
                }

                return _value;
            }
            set
            {
                if (!Loaded)
                {
                    Load();
                }

                _value = value;
            }
        }

        internal override void Clear()
        {
            Loaded = false;
            if (typeof(TEntity) == typeof(byte[]))
            {
                _originalValue = (TEntity) (object) new byte[0];
            }
            else
            {
                _originalValue = default(TEntity);
            }

            _value = _originalValue;
        }

        internal override Dictionary<StatePath, StateValue> GetChanges()
        {
            var dict = new Dictionary<StatePath, StateValue>();
            if (!Equals(_originalValue, _value))
            {
                dict[Path] = new StateValue()
                {
                    OriginalValue = ByteString.CopyFrom(SerializationHelpers.Serialize(_originalValue)),
                    CurrentValue = ByteString.CopyFrom(SerializationHelpers.Serialize(_value))
                };
            }

            return dict;
        }

        private void Load()
        {
            var bytes = Provider.GetAsync(Path).Result;
            _originalValue = SerializationHelpers.Deserialize<TEntity>(bytes);
            _value = _originalValue;
            Loaded = true;
        }
    }
}