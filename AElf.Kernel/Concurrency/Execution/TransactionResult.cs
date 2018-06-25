using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class TransactionResult : ISerializable
    {
        public byte[] Serialize()
        {
            return this.ToByteArray();
        }
    }
}