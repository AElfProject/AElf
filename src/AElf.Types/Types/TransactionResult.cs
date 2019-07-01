using Google.Protobuf;

namespace AElf.Types
{
    public partial class TransactionResult
    {
        private static readonly ByteString _empty =ByteString.CopyFrom(new byte[256]);
        partial void OnConstruction()
        {
            Bloom = _empty;
        }

        public byte[] Serialize()
        {
            return this.ToByteArray();
        }
    }
}