using AElf.Types;
using Google.Protobuf;

namespace AElf.Sdk.CSharp.State
{
    public class ReadonlyState : StateBase
    {
    }

    public class ReadonlyState<TEntity> : ReadonlyState
    {
        internal bool Loaded;

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

                if (_value == null)
                {
                    _value = value;
                }
            }
        }

        internal override void Clear()
        {
            Loaded = false;
            _value = default;
        }

        internal override TransactionExecutingStateSet GetChanges()
        {
            var stateSet = new TransactionExecutingStateSet();
            var key = Path.ToStateKey(Context.Self);
            if (_value != null)
            {
                stateSet.Writes[key] = ByteString.CopyFrom(SerializationHelper.Serialize(_value));
            }

            //if (Loaded) stateSet.Reads[key] = true;

            return stateSet;
        }

        private void Load()
        {
            var bytes = Provider.GetAsync(Path).Result;
            _value = SerializationHelper.Deserialize<TEntity>(bytes);
            Loaded = true;
        }
    }
}