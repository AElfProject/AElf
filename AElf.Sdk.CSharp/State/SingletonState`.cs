using AElf.Kernel;
using AElf.Kernel.SmartContract.Sdk;
using Google.Protobuf;

namespace AElf.Sdk.CSharp.State
{
    public class SingletonState : StateBase
    {
    }

    public class SingletonState<TEntity> : SingletonState
    {
        private TEntity _originalValue;
        private TEntity _value;
        internal bool Loaded;
        internal bool Modified => Equals(_originalValue, _value);

        public TEntity Value
        {
            get
            {
                if (!Loaded) Load();

                return _value;
            }
            set
            {
                if (!Loaded) Load();

                _value = value;
            }
        }

        internal override void Clear()
        {
            Loaded = false;
            if (typeof(TEntity) == typeof(byte[]))
                _originalValue = (TEntity) (object) new byte[0];
            else
                _originalValue = default(TEntity);

            _value = _originalValue;
        }

        internal override TransactionExecutingStateSet GetChanges()
        {
            var stateSet = new TransactionExecutingStateSet();
            if (!Equals(_originalValue, _value))
                stateSet.Writes[Path.ToStateKey()] = ByteString.CopyFrom(SerializationHelper.Serialize(_value));

            return stateSet;
        }

        private void Load()
        {
            var bytes = Provider.GetAsync(Path).Result;
            _originalValue = SerializationHelper.Deserialize<TEntity>(bytes);
            _value = SerializationHelper.Deserialize<TEntity>(bytes);
            Loaded = true;
        }

        private void UpdateToCache(TEntity value)
        {
            Provider.Cache[Path] = SerializationHelper.Serialize(value);
        }
    }
}