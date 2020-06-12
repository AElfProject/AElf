using AElf.Types;
using AElf.Kernel.SmartContract;
using Google.Protobuf;

namespace AElf.Sdk.CSharp.State
{
    public class StateBase
    {
        private StatePath _path;
        private CSharpSmartContractContext _context;
        internal IStateProvider Provider => _context.StateProvider;

        internal StatePath Path
        {
            get => _path;
            set
            {
                _path = value;
                OnPathSet();
            }
        }

        internal CSharpSmartContractContext Context
        {
            get => _context;
            set
            {
                _context = value;
                OnContextSet();
            }
        }

        internal virtual void OnPathSet()
        {
        }

        internal virtual void OnContextSet()
        {
        }

        internal virtual void Clear()
        {
        }

        internal virtual TransactionExecutingStateSet GetChanges()
        {
            return new TransactionExecutingStateSet();
        }

        internal ByteString CheckAndReturnSerializedValue(string key, object value)
        {
            var serializedValue = SerializationHelper.Serialize(value);
            if (serializedValue.Length > SmartContractConstants.AElfStateSizeLimitInContract)
            {
                throw new StateSizeExceededException($"The size of new value of {key} is exceeded.");
            }

            return ByteString.CopyFrom(serializedValue);
        }
    }
}