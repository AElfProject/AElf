using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class TransactionResult
    {
        // TODO: use Bloom.Empty to init
        partial void OnConstruction()
        {
            Bloom = ByteString.CopyFrom(new Bloom().Data);
        }

        // TODO: remove
        public byte[] Serialize()
        {
            return this.ToByteArray();
        }
    }
}