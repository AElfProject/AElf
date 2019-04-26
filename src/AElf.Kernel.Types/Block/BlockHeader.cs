using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel
{
    public partial class BlockHeader : IBlockHeader
    {
        partial void OnConstruction()
        {
            // TODO: improve perf, Bloom.Empty
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
            var block = this.Clone();
            block.Signature = ByteString.Empty;
            return block.ToByteArray();
        }
    }
}