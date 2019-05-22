using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel
{
    public partial class BlockHeader : IBlockHeader
    {
        partial void OnConstruction()
        {
            Bloom = ByteString.CopyFrom(new Bloom().Data);
        }

        public BlockHeader(Hash preBlockHash)
        {
            PreviousBlockHash = preBlockHash;
        }

        public Hash GetHash()
        {
            return Hash.FromRawBytes(GetSignatureData());
        }

        public byte[] GetHashBytes()
        {
            return GetHash().DumpByteArray();
        }

        private byte[] GetSignatureData()
        {
            if (Signature.IsEmpty)
                return this.ToByteArray();

            var blockHeader = Clone();
            blockHeader.Signature = ByteString.Empty;
            return blockHeader.ToByteArray();
        }
    }
}