using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class BlockHeader : IBlockHeader
    {
        partial void OnConstruction()
        {
            Bloom = ByteString.CopyFrom(new Bloom().Data);
        }

        private Hash _blockHash;

        public BlockHeader(Hash preBlockHash)
        {
            PreviousBlockHash = preBlockHash;
        }

        public Hash GetHash()
        {
            if (_blockHash == null)
            {
                _blockHash = Hash.FromRawBytes(GetSignatureData());
            }

            return _blockHash;
        }

        public Hash GetHashWithoutCache()
        {
            _blockHash = null;
            return GetHash();
        }

        public byte[] GetHashBytes()
        {
            if (_blockHash == null)
                _blockHash = Hash.FromRawBytes(GetSignatureData());

            return _blockHash.DumpByteArray();
        }

        private byte[] GetSignatureData()
        {
            if (this.Signature.IsEmpty)
                return this.ToByteArray();
            var blockHeader = this.Clone();
            blockHeader.Signature = ByteString.Empty;
            return blockHeader.ToByteArray();
         }
    }
}