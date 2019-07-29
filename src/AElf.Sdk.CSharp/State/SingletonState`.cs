using AElf.Kernel;
using AElf.Types;
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
            _originalValue = default(TEntity);
            _value = _originalValue;
        }

        internal override TransactionExecutingStateSet GetChanges()
        {
            var stateSet = new TransactionExecutingStateSet();
            var key = Path.ToStateKey(Context.Self);
            if (!Equals(_originalValue, _value))
            {
                stateSet.Writes[key] = ByteString.CopyFrom(SerializationHelper.Serialize(_value));
            }

            if (Loaded) stateSet.Reads[key] = true;

            return stateSet;
        }

        private void Load()
        {
            var bytes = Provider.GetAsync(Path).Result;
            _originalValue = SerializationHelper.Deserialize<TEntity>(bytes);
            _value = SerializationHelper.Deserialize<TEntity>(bytes);
            Loaded = true;
        }
    }
}