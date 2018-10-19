using AElf.Common;
using Google.Protobuf;

namespace AElf.Kernel
{
    public partial class StateValue
    {
        public bool IsDirty => OriginalValue != CurrentValue;

        public static StateValue Create(byte[] value)
        {
            var sc = new StateValue();
            if (value != null)
            {
                sc.OriginalValue = ByteString.CopyFrom(value);
                sc.CurrentValue = ByteString.CopyFrom(value);
            }

            return sc;
        }

        public byte[] Get()
        {
            return CurrentValue?.ToByteArray();
        }

        public void Set(byte[] value)
        {
            CurrentValue = ByteString.CopyFrom(value);
        }
    }
}