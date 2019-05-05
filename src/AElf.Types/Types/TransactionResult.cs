using Google.Protobuf;

namespace AElf.Types
{
    public partial class TransactionResult
    {
        partial void OnConstruction()
        {
            Bloom = ByteString.CopyFrom(new byte[256]);
        }

        public byte[] Serialize()
        {
            return this.ToByteArray();
        }
    }
}