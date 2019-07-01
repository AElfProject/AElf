using Google.Protobuf;

namespace AElf.Types
{
    public partial class TransactionResult
    {
        private static readonly byte[] _empty = new byte[256];
        partial void OnConstruction()
        {
            Bloom = ByteString.CopyFrom(_empty);
        }

        public byte[] Serialize()
        {
            return this.ToByteArray();
        }
    }
}