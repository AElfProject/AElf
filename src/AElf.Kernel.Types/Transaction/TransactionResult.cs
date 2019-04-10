using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class TransactionResult 
    {
        partial void OnConstruction()
        {
            Bloom = ByteString.CopyFrom(new Bloom().Data);
        }

        public byte[] Serialize()
        {
            return this.ToByteArray();
        }
    }
}