using AElf.Common;
using Google.Protobuf;

namespace AElf.Kernel
{
    public partial class StateChange
    {
        public bool IsDirty => OriginalValue != CurrentValue;

        public static StateChange Create(byte[] value)
        {
            return new StateChange()
            {
                OriginalValue = ByteString.CopyFrom(value),
                CurrentValue = ByteString.CopyFrom(value)
            };
        }

        public byte[] Get()
        {
            return CurrentValue.ToByteArray();
        }

        public void Set(byte[] value)
        {
            CurrentValue = ByteString.CopyFrom(value);
        }
    }
}