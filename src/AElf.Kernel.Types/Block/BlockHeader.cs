using System;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel
{
    public partial class BlockHeader : IBlockHeader
    {
        private Hash _blockHash;

        public BlockHeader(Hash preBlockHash)
        {
            PreviousBlockHash = preBlockHash;
        }

        public Hash GetHash()
        {
            if (_blockHash == null)
                _blockHash = Hash.FromRawBytes(GetSignatureData());

            return _blockHash;
        }

        public Hash GetHashWithoutCache()
        {
            _blockHash = null;
            return GetHash();
        }

        public byte[] GetHashBytes()
        {
            return GetHash().ToByteArray();
        }

        private byte[] GetSignatureData()
        {
            if (!VerifyFields())
                throw new InvalidOperationException($"Invalid block header: {this}.");

            if (Signature.IsEmpty)
                return this.ToByteArray();

            var blockHeader = Clone();
            blockHeader.Signature = ByteString.Empty;
            return blockHeader.ToByteArray();
        }

        public bool VerifyFields()
        {
            if (ChainId < 0)
                return false;

            if (Height < Constants.GenesisBlockHeight)
                return false;

            if (Height > Constants.GenesisBlockHeight && SignerPubkey.IsEmpty)
                return false;

            if (PreviousBlockHash == null)
                return false;

            if (MerkleTreeRootOfTransactions == null || MerkleTreeRootOfWorldState == null ||
                MerkleTreeRootOfTransactionStatus == null)
                return false;

            if (Time == null)
                return false;

            return true;
        }
    }
}