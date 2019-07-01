using System;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel
{
    public partial class BlockHeader : IBlockHeader
    {
        private Hash _blockHash;

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
            return GetHash().DumpByteArray();
        }

        private byte[] GetSignatureData()
        {
            if (ChainId < 0 ||
                PreviousBlockHash == null ||
                MerkleTreeRootOfTransactions == null ||
                MerkleTreeRootOfWorldState == null ||
                Height <= 0 ||
                Time == null ||
                MerkleTreeRootOfTransactionStatus == null ||
                Height >  Constants.GenesisBlockHeight && (ExtraData?.Count == 0 || SignerPubkey.IsEmpty))
            {
                throw new InvalidOperationException($"Invalid block header: {this}");
            }

            if (Signature.IsEmpty)
                return this.ToByteArray();

            var blockHeader = Clone();
            blockHeader.Signature = ByteString.Empty;
            return blockHeader.ToByteArray();
        }
    }
}