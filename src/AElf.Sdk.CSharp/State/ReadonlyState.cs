using AElf.Types;
using Google.Protobuf;

namespace AElf.Sdk.CSharp.State;

public class ReadonlyState : StateBase
{
}

public class ReadonlyState<TEntity> : ReadonlyState
{
    private TEntity _value;
    internal bool Loaded;

    // If the target of current ReadonlyState is default(TEntity), it maybe not proper to use ReadonlyState.
    private bool NotSetBefore => Equals(_value, default(TEntity));

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

            if (NotSetBefore) _value = value;
        }
    }

    public override void Clear()
    {
        Loaded = false;
        _value = default;
    }

    public override TransactionExecutingStateSet GetChanges()
    {
        var stateSet = new TransactionExecutingStateSet();
        var key = Path.ToStateKey(Context.Self);
        if (!NotSetBefore) stateSet.Writes[key] = ByteString.CopyFrom(SerializationHelper.Serialize(_value));

        return stateSet;
    }

    private void Load()
    {
        var bytes = Provider.Get(Path);
        _value = SerializationHelper.Deserialize<TEntity>(bytes);
        Loaded = true;
    }
}